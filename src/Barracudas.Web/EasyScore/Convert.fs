module Barracudas.Web.EasyScore.Convert

open System
open System.Globalization
open FsToolkit.ErrorHandling
open Barracudas.Web.Domain
open Barracudas.Web.EasyScore.Dto
open Barracudas.Web.EasyScore.Api

let private inv = CultureInfo.InvariantCulture

/// "2026-05-02T12:30:00.000Z" → the literal wall-clock value (no tz shift —
/// the feed stores local Swiss times with a fake Z suffix).
let private parseDateTime (s: string) : DateTime option =
    match DateTimeOffset.TryParse(s, inv, DateTimeStyles.None) with
    | true, v -> Some v.DateTime
    | _ -> None

/// Game date = GameDate's date + StartTime's time of day (StartTime is
/// sometimes anchored to 1900-01-01, so only its time component is reliable).
let private gameDate (g: GameDto) : DateTime =
    let date = parseDateTime g.GameDate |> Option.map _.Date |> Option.defaultValue DateTime.MinValue
    let time = g.StartTime |> Option.bind parseDateTime |> Option.map _.TimeOfDay |> Option.defaultValue TimeSpan.Zero
    date.Add time

let private isOver (g: GameDto) = g.GameEnded = 1

/// Live=1 means "live-scored", which persists after the game; in progress is
/// the combination of live-scored and not yet ended (and not postponed).
let private inProgress (g: GameDto) = g.Live = 1 && g.GameEnded = 0 && g.postponed = 0

let private statusOf (g: GameDto) =
    if isOver g then Final
    elif inProgress g then Live
    else Scheduled

let private involves (teamId: int) (g: GameDto) = g.AwayTeam = teamId || g.HomeTeam = teamId

/// Pick our round (e.g. "1.Liga Baseball Ost 2026") out of the league's rounds.
let findRound (nameFilter: string) (rounds: RoundDto list) : Async<Result<RoundDto, EasyScoreError>> =
    rounds
    |> List.tryFind _.Round.Contains(nameFilter, StringComparison.OrdinalIgnoreCase)
    |> Result.requireSome (ConvertError $"no round matching '%s{nameFilter}' among %d{rounds.Length} rounds")
    |> Async.singleton

/// Our games from the round's full schedule, sorted by date.
let toGames (teamId: int) (dtos: GameDto list) : Async<Result<Game list, EasyScoreError>> =
    asyncResult {
        return
            [ for g in dtos do
                if involves teamId g then
                    let status = statusOf g
                    { Id = string g.ID
                      Date = gameDate g
                      Away = { Name = g.AwayTeamName; Logo = g.AwayLogo }
                      Home = { Name = g.HomeTeamName; Logo = g.HomeLogo }
                      IsHome = g.HomeTeam = teamId
                      Location = defaultArg g.Field ""
                      Status = status
                      AwayScore = (if status = Scheduled then None else g.AwayRuns)
                      HomeScore = (if status = Scheduled then None else g.HomeRuns)
                      BoxScoreUrl =
                        if status = Final && g.BoxScoreGenerated = 1 then
                            Some $"https://www.easyscore.com/boxscores/%d{g.ID}"
                        else
                            None } ]
            |> List.sortBy _.Date
    }

/// League standings computed from the round's completed games.
let toStandings (teamId: int) (teams: TeamDto list) (games: GameDto list) : Async<Result<Standing list, EasyScoreError>> =
    asyncResult {
        let finals = games |> List.filter isOver |> List.sortBy gameDate
        let abbrs =
            (Map.empty, finals)
            ||> List.fold (fun m g -> m |> Map.add g.AwayTeam g.AwayTeamNameShort |> Map.add g.HomeTeam g.HomeTeamNameShort)
        // Some true = win, Some false = loss for the given team; ties/no result = None.
        let resultFor (id: int) (g: GameDto) : bool option =
            match g.AwayRuns, g.HomeRuns with
            | Some a, Some h when a <> h && involves id g -> Some(if g.AwayTeam = id then a > h else h > a)
            | _ -> None
        let pctOf w l = if w + l = 0 then 0.0 else float w / float (w + l)
        let rows =
            [ for t in teams ->
                let results = finals |> List.choose (resultFor t.Team)
                let wins = results |> List.filter id |> List.length
                let losses = results.Length - wins
                let streak =
                    match List.rev results with
                    | [] -> "—"
                    | last :: rest ->
                        sprintf "%s%d" (if last then "W" else "L") (1 + List.length (List.takeWhile ((=) last) rest))
                t, wins, losses, streak ]
            |> List.sortByDescending (fun (_, w, l, _) -> pctOf w l, w)
        return
            rows
            |> List.mapi (fun i (t, w, l, streak) ->
                let gb =
                    match rows with
                    | (_, lw, ll, _) :: _ -> float ((lw - w) + (l - ll)) / 2.0
                    | [] -> 0.0
                { Rank = i + 1
                  Team = t.Name
                  Abbr = abbrs |> Map.tryFind t.Team |> Option.defaultValue ""
                  Games = w + l
                  Wins = w
                  Losses = l
                  Pct = pctOf w l
                  GamesBehind = gb
                  Streak = streak
                  IsUs = t.Team = teamId })
    }

/// Season summary cards from our standings row + our completed games.
let toTeamStats (standings: Standing list) (ourGames: Game list) : Async<Result<TeamStat list, EasyScoreError>> =
    asyncResult {
        let finals = ourGames |> List.filter (fun g -> g.Status = Final)
        let rs = finals |> List.sumBy (fun g -> defaultArg g.OurScore 0)
        let ra = finals |> List.sumBy (fun g -> defaultArg g.OpponentScore 0)
        let diff = rs - ra
        return
            [ match standings |> List.tryFind _.IsUs with
              | Some s ->
                  { Label = "Record"; Value = $"%d{s.Wins}–%d{s.Losses}" }
                  { Label = "Win %"; Value = (let p = s.Pct.ToString("0.000", inv) in if p.StartsWith "0" then p.Substring 1 else p) }
                  { Label = "Streak"; Value = s.Streak }
              | None -> ()
              { Label = "Runs Scored"; Value = string rs }
              { Label = "Runs Allowed"; Value = string ra }
              { Label = "Run Diff"; Value = (if diff >= 0 then $"+%d{diff}" else string diff) } ]
    }

/// Our roster: the configured active-roster licences. Batting averages come
/// from the round's offensive stats (players without at-bats have no stats row).
let toRoster (activeRoster: int list) (dtos: PlayerDto list) (offense: OffenseStatsDto list) : Async<Result<Player list, EasyScoreError>> =
    asyncResult {
        let active = Set.ofList activeRoster
        let avgs = offense |> List.map (fun s -> s.PlayerID, s.BA) |> Map.ofList
        return
            [ for p in dtos do
                if active.Contains p.ID then
                    { Id = string p.ID
                      FirstName = defaultArg p.Name ""
                      LastName = defaultArg p.Lastname ""
                      Number = p.UniformNr |> Option.bind (fun n -> match Int32.TryParse n with | true, v -> Some v | _ -> None)
                      Bats = defaultArg p.Bats ""
                      Throws = defaultArg p.Throws ""
                      BattingAvg = avgs |> Map.tryFind p.ID } ]
            |> List.sortBy (fun p -> p.LastName.ToUpperInvariant(), p.FirstName.ToUpperInvariant())
    }

/// A player's season stat lines, picked out of the round-wide stat lists.
let toPlayerStats
    (playerId: int)
    (offense: OffenseStatsDto list)
    (fielding: FieldingStatsDto list)
    (pitching: PitchingStatsDto list)
    : Async<Result<PlayerStats, EasyScoreError>> =
    asyncResult {
        let batting =
            offense
            |> List.tryFind (fun s -> s.PlayerID = playerId)
            |> Option.map (fun s ->
                { Games = s.G
                  PA = s.PA
                  AB = s.AB
                  R = s.R
                  H = s.H
                  Doubles = s.Doubles
                  Triples = s.Triples
                  HR = s.HR
                  RBI = s.RBI
                  TB = s.TB
                  BB = s.BB
                  SO = s.SO
                  HBP = s.HBP
                  SB = s.SB
                  CS = s.CS
                  AVG = s.BA
                  OBP = s.OBP
                  SLG = s.SLG
                  OPS = s.OPS })
        let field =
            fielding
            |> List.tryFind (fun s -> s.PlayerID = playerId)
            |> Option.map (fun s ->
                { Games = s.G
                  Innings = s.InningsPlayed
                  Putouts = s.Putout
                  Assists = s.Assist
                  OutfieldAssists = s.OutfieldAssists
                  Errors = s.Error
                  DoublePlays = s.DP
                  PassedBalls = s.PB
                  StealAttempts = s.SBAtt
                  CaughtStealing = s.CSMade
                  RangeFactor = s.RangeFactor
                  FieldingPct = s.FPct })
        let pitch =
            pitching
            |> List.tryFind (fun s -> s.PlayerID = playerId)
            |> Option.map (fun s ->
                { Games = s.G
                  Starts = s.GS
                  IP = s.IP
                  H = s.HA
                  R = s.RA
                  ER = s.ER
                  BB = s.BBA
                  SO = s.K
                  HBP = s.HBPA
                  WildPitches = s.WP
                  Record = $"%s{s.W}–%s{s.L}"
                  Saves = s.SV
                  BattersFaced = s.BF
                  OppAVG = s.OppAVG
                  WHIP = s.WHIP
                  ERA = s.ERA })
        return { Batting = batting; Fielding = field; Pitching = pitch }
    }

/// The in-progress game involving us, if any.
let toLiveGame (teamId: int) (games: GameDto list) : Async<Result<LiveGame option, EasyScoreError>> =
    asyncResult {
        return
            games
            |> List.tryFind (fun g -> inProgress g && involves teamId g)
            |> Option.map (fun g ->
                { GameId = string g.ID
                  AwayName = g.AwayTeamName
                  HomeName = g.HomeTeamName })
    }
