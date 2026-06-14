module Barracudas.Web.EasyScore.Convert

open System
open System.Globalization
open System.Text.Json
open FsToolkit.ErrorHandling
open Barracudas.Web.Domain
open Barracudas.Web.EasyScore.Dto
open Barracudas.Web.EasyScore.Api
open Microsoft.Extensions.Logging

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
    let date =
        parseDateTime g.GameDate
        |> Option.map _.Date
        |> Option.defaultValue DateTime.MinValue

    let time =
        g.StartTime
        |> Option.bind parseDateTime
        |> Option.map _.TimeOfDay
        |> Option.defaultValue TimeSpan.Zero

    date.Add time

let private isOver (g: GameDto) = g.GameEnded = 1

/// Live=1 means "live-scored", which persists after the game; in progress is
/// the combination of live-scored and not yet ended (and not postponed).
let private inProgress (g: GameDto) =
    g.Live = 1 && g.GameEnded = 0 && g.postponed = 0

let private statusOf (g: GameDto) =
    if isOver g then Final
    elif inProgress g then Live
    else Scheduled

let private involves (teamId: int) (g: GameDto) =
    g.AwayTeam = teamId || g.HomeTeam = teamId

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
                        Away =
                          { Name = g.AwayTeamName
                            Logo = g.AwayLogo }
                        Home =
                          { Name = g.HomeTeamName
                            Logo = g.HomeLogo }
                        IsHome = g.HomeTeam = teamId
                        Location = defaultArg g.Field ""
                        Status = status
                        AwayScore = (if status = Scheduled then None else g.AwayRuns)
                        HomeScore = (if status = Scheduled then None else g.HomeRuns)
                        BoxScoreUrl =
                          if status = Final && g.BoxScoreGenerated = 1 then
                              Some $"/boxscore/%d{g.ID}"
                          else
                              None } ]
            |> List.sortBy _.Date
    }

/// League standings computed from the round's completed games.
let toStandings
    (teamId: int)
    (teams: TeamDto list)
    (games: GameDto list)
    : Async<Result<Standing list, EasyScoreError>> =
    asyncResult {
        let finals = games |> List.filter isOver |> List.sortBy gameDate

        let abbrs =
            (Map.empty, finals)
            ||> List.fold (fun m g ->
                m
                |> Map.add g.AwayTeam g.AwayTeamNameShort
                |> Map.add g.HomeTeam g.HomeTeamNameShort)
        // Some true = win, Some false = loss for the given team; ties/no result = None.
        let resultFor (id: int) (g: GameDto) : bool option =
            match g.AwayRuns, g.HomeRuns with
            | Some a, Some h when a <> h && involves id g -> Some(if g.AwayTeam = id then a > h else h > a)
            | _ -> None

        let pctOf w l =
            if w + l = 0 then 0.0 else float w / float (w + l)

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
                  { Label = "Record"
                    Value = $"%d{s.Wins}–%d{s.Losses}" }

                  { Label = "Win %"
                    Value = (let p = s.Pct.ToString("0.000", inv) in if p.StartsWith "0" then p.Substring 1 else p) }

                  { Label = "Streak"; Value = s.Streak }
              | None -> ()
              { Label = "Runs Scored"
                Value = string rs }
              { Label = "Runs Allowed"
                Value = string ra }
              { Label = "Run Diff"
                Value = (if diff >= 0 then $"+%d{diff}" else string diff) } ]
    }

/// Our roster: the configured active-roster licences. Batting averages come
/// from the round's offensive stats (players without at-bats have no stats row).
let toRoster
    (activeRoster: int list)
    (dtos: PlayerDto list)
    (offense: OffenseStatsDto list)
    : Async<Result<Player list, EasyScoreError>> =
    asyncResult {
        let active = Set.ofList activeRoster

        let avgs =
            offense
            |> List.map (fun s -> s.PlayerID, s.BA |> Option.defaultValue "")
            |> Map.ofList

        return
            [ for p in dtos do
                  if active.Contains p.ID then
                      { Id = string p.ID
                        FirstName = defaultArg p.Name ""
                        LastName = defaultArg p.Lastname ""
                        Number =
                          p.UniformNr
                          |> Option.bind (fun n ->
                              match Int32.TryParse n with
                              | true, v -> Some v
                              | _ -> None)
                        Bats = defaultArg p.Bats ""
                        Throws = defaultArg p.Throws ""
                        BattingAvg = avgs |> Map.tryFind p.ID } ]
            |> List.sortBy (fun p -> p.LastName.ToUpperInvariant(), p.FirstName.ToUpperInvariant())
    }

/// Log dates carry no reliable time component; only the date matters.
let private logDate (s: string) =
    parseDateTime s |> Option.map _.Date |> Option.defaultValue DateTime.MinValue

let private logWarningDefaultEmpty (logger: ILogger) playerId what =
    what
    |> Option.defaultWith (fun _ ->
        logger.LogWarning("Unable to parse {What} for player {Player}; defaulting to \"\"", nameof what, playerId)
        "")

let private logWarningDefaultZero (logger: ILogger) playerId what =
    what
    |> Option.defaultWith (fun _ ->
        logger.LogWarning("Unable to parse {What} for player {Player}; defaulting to 0", nameof what, playerId)
        0)

/// A player's season stat lines, picked out of the round-wide stat lists,
/// plus the per-game logs (already filtered to the player by the API).
let toPlayerStats
    (logger: ILogger)
    (offense: OffenseStatsDto list)
    (fielding: FieldingStatsDto list)
    (pitching: PitchingStatsDto list)
    (battingLog: BattingLogDto list)
    (fieldingLog: FieldingLogDto list)
    (pitchingLog: PitchingLogDto list)
    (playerId: int)
    : Async<Result<PlayerStats, EasyScoreError>> =
    asyncResult {
        let batting =
            offense
            |> List.tryFind (fun s -> s.PlayerID = playerId)
            |> Option.map (fun s ->
                { Games = s.G |> logWarningDefaultZero logger playerId
                  PA = s.PA |> logWarningDefaultEmpty logger playerId
                  AB = s.AB |> logWarningDefaultEmpty logger playerId
                  R = s.R |> logWarningDefaultEmpty logger playerId
                  H = s.H |> logWarningDefaultEmpty logger playerId
                  Doubles = s.Doubles |> logWarningDefaultEmpty logger playerId
                  Triples = s.Triples |> logWarningDefaultEmpty logger playerId
                  HR = s.HR |> logWarningDefaultEmpty logger playerId
                  RBI = s.RBI |> logWarningDefaultEmpty logger playerId
                  TB = s.TB |> logWarningDefaultEmpty logger playerId
                  BB = s.BB |> logWarningDefaultEmpty logger playerId
                  SO = s.SO |> logWarningDefaultEmpty logger playerId
                  HBP = s.HBP |> logWarningDefaultEmpty logger playerId
                  SB = s.SB |> logWarningDefaultEmpty logger playerId
                  CS = s.CS |> logWarningDefaultEmpty logger playerId
                  AVG = s.BA |> logWarningDefaultEmpty logger playerId
                  OBP = s.OBP |> logWarningDefaultEmpty logger playerId
                  SLG = s.SLG |> logWarningDefaultEmpty logger playerId
                  OPS = s.OPS |> logWarningDefaultEmpty logger playerId })

        let field =
            fielding
            |> List.tryFind (fun s -> s.PlayerID = playerId)
            |> Option.map (fun s ->
                { Games = s.G |> logWarningDefaultZero logger playerId
                  Innings = s.InningsPlayed |> logWarningDefaultEmpty logger playerId
                  Putouts = s.Putout |> logWarningDefaultEmpty logger playerId
                  Assists = s.Assist |> logWarningDefaultEmpty logger playerId
                  OutfieldAssists = s.OutfieldAssists |> logWarningDefaultEmpty logger playerId
                  Errors = s.Error |> logWarningDefaultEmpty logger playerId
                  DoublePlays = s.DP |> logWarningDefaultEmpty logger playerId
                  PassedBalls = s.PB |> logWarningDefaultEmpty logger playerId
                  StealAttempts = s.SBAtt |> logWarningDefaultEmpty logger playerId
                  CaughtStealing = s.CSMade |> logWarningDefaultEmpty logger playerId
                  RangeFactor = s.RangeFactor |> logWarningDefaultEmpty logger playerId
                  FieldingPct = s.FPct |> logWarningDefaultEmpty logger playerId })

        let pitch =
            pitching
            |> List.tryFind (fun s -> s.PlayerID = playerId)
            |> Option.map (fun s ->
                { Games = s.G |> logWarningDefaultEmpty logger playerId
                  Starts = s.GS |> logWarningDefaultEmpty logger playerId
                  IP = s.IP |> logWarningDefaultEmpty logger playerId
                  H = s.HA |> logWarningDefaultEmpty logger playerId
                  R = s.RA |> logWarningDefaultEmpty logger playerId
                  ER = s.ER |> logWarningDefaultEmpty logger playerId
                  BB = s.BBA |> logWarningDefaultEmpty logger playerId
                  SO = s.K |> logWarningDefaultEmpty logger playerId
                  HBP = s.HBPA |> logWarningDefaultEmpty logger playerId
                  WildPitches = s.WP |> logWarningDefaultEmpty logger playerId
                  Record =
                    $"%s{s.W |> logWarningDefaultEmpty logger playerId}–%s{s.L |> logWarningDefaultEmpty logger playerId}"
                  Saves = s.SV |> logWarningDefaultEmpty logger playerId
                  BattersFaced = s.BF |> logWarningDefaultEmpty logger playerId
                  OppAVG = s.OppAVG |> logWarningDefaultEmpty logger playerId
                  WHIP = s.WHIP |> logWarningDefaultEmpty logger playerId
                  ERA = s.ERA |> logWarningDefaultEmpty logger playerId })

        let batLog =
            [ for e in battingLog ->
                  { Date = logDate e.Date
                    Opponent = e.Opponent
                    Spot = e.Spot
                    Pos = e.Pos
                    AB = e.AB
                    R = e.R
                    H = e.H
                    Doubles = e.Doubles
                    Triples = e.Triples
                    HR = e.HR
                    RBI = e.RBI
                    BB = e.BB
                    SO = e.SO
                    SB = e.SB
                    CS = e.CS
                    HBP = e.HBP
                    Sac = e.S
                    SacFlies = e.SF
                    GIDP = e.GIDP
                    TwoOutRBI = e.TwoOutRBI
                    RISP = e.RISP
                    GameScore = e.Gsc
                    AvgToDate = e.BA } ]

        let fldLog =
            [ for e in fieldingLog ->
                  { Date = logDate e.Date
                    Opponent = e.Opponent
                    Pos = e.Pos
                    Innings = e.InningsPlayed
                    Putouts = e.Putout
                    Assists = e.Assist
                    OutfieldAssists = e.OutfieldAssists
                    Errors = e.Error
                    DoublePlays = e.DP
                    PassedBalls = e.PB
                    StealAttempts = e.SBAtt
                    CaughtStealing = e.CSMade
                    RangeFactorToDate = e.RangeFactor
                    FieldingPctToDate = e.FPct } ]

        let pitLog =
            [ for e in pitchingLog ->
                  { Date = logDate e.Date
                    Opponent = e.Opponent
                    IP = e.IP
                    H = e.H
                    R = e.R
                    ER = e.ER
                    BB = e.BB
                    SO = e.K
                    HBP = e.HBP
                    WildPitches = e.WP
                    Balks = e.BK
                    GroundBalls = e.GB
                    FlyBalls = e.FB
                    BattersFaced = e.BF
                    Pitches = e.Pitches
                    Decision = e.Decision
                    Relief = e.Relief
                    GameScore = e.GSc
                    WhipToDate = e.WHIP
                    EraToDate = e.ERA } ]

        return
            { Batting = batting
              Fielding = field
              Pitching = pitch
              BattingLog = batLog
              FieldingLog = fldLog
              PitchingLog = pitLog }
    }

let private blankToNone (s: string) =
    if String.IsNullOrWhiteSpace s then None else Some s

/// A linescore cell is a run count (number) or "x" (a side that didn't bat).
let private lineCell (e: JsonElement) =
    match e.ValueKind with
    | JsonValueKind.Number -> string (e.GetInt32())
    | JsonValueKind.String -> e.GetString()
    | _ -> ""

let private toLineTeam (s: LineScoreSideDto) (innings: int) : LineScoreTeam =
    { Name = s.TeamName
      Abbr = s.TeamAbbreviation
      Logo = s.logo |> Option.bind blankToNone
      Innings =
        [ for i in 1..innings ->
              s.line
              |> Map.tryFind (string i)
              |> Option.map lineCell
              |> Option.defaultValue "" ]
      Runs = s.totals.R
      Hits = s.totals.H
      Errors = s.totals.E }

let private toBatter logger (h: BoxHitterDto) : BoxBatter =
    { Order = (if h.Spot = 100 then None else Some h.Spot)
      PlayerId = h.playerID
      IsSub = h.SubbedIn |> Option.bind blankToNone |> Option.isSome
      Pos = h.Pos
      Name = h.playerName
      AB = h.AB |> logWarningDefaultZero logger h.playerID
      R = h.R |> logWarningDefaultZero logger h.playerID
      H = h.H |> logWarningDefaultZero logger h.playerID
      RBI = h.RBI |> logWarningDefaultZero logger h.playerID
      BB = h.BB |> logWarningDefaultZero logger h.playerID
      SO = h.SO |> logWarningDefaultZero logger h.playerID
      LOB = h.LOB |> logWarningDefaultZero logger h.playerID
      Avg = defaultArg h.BA "" }

let private toPitcher (p: BoxPitcherDto) : BoxPitcher =
    let isTotals = p.PitcherNr = 100

    { Name = (if isTotals then "Totals" else p.playerName)
      IsTotals = isTotals
      IP = p.IP
      H = p.HA
      R = p.RA
      ER = p.ER
      BB = p.BBA
      SO = p.K
      HR = p.HRA
      BattersFaced = p.BF
      Pitches = p.PitchCount
      Strikes = p.Strikes
      ERA = p.ERA }

/// Build a game note (away = T, home = B); None when neither side has text.
let private boxNote (label: string) (d: BoxNoteDto option) : BoxNote option =
    match d with
    | Some n ->
        let away = n.T |> Option.bind blankToNone
        let home = n.B |> Option.bind blankToNone

        if away.IsSome || home.IsSome then
            Some
                { Label = label
                  Away = away
                  Home = home }
        else
            None
    | None -> None

/// A completed game's box score: linescore from /games, the rest from /stats?box.
let toBoxScore
    (logger: ILogger)
    (teamId: int)
    (gameId: int)
    (opponentColor: string option)
    (detail: GameDetailDto list)
    (response: BoxScoreResponseDto list)
    : Async<Result<BoxScore option, EasyScoreError>> =
    asyncResult {
        match response |> List.tryHead |> Option.map _.BoxScores with
        | None -> return None
        | Some b ->
            let head = detail |> List.tryHead

            let awayIsUs =
                head |> Option.map (fun d -> d.AwayTeam = teamId) |> Option.defaultValue false

            let homeIsUs =
                head |> Option.map (fun d -> d.HomeTeam = teamId) |> Option.defaultValue false

            let side (s: string) =
                [ for h in b.AllHitters do
                      if h.TopOrBot = s then
                          h ]

            let pitchers (s: string) =
                [ for p in b.AllPitchers do
                      if p.TopOrBot = s then
                          toPitcher p ]

            let lineScore =
                head
                |> Option.bind (fun d -> d.LineScore |> List.tryHead)
                |> Option.map (fun ls ->
                    let inningsPlayed =
                        if ls.innings > 0 then
                            ls.innings
                        else
                            List.max [ ls.away.line.Count; ls.home.line.Count; 1 ]

                    { Innings = inningsPlayed
                      Away = toLineTeam ls.away inningsPlayed
                      Home = toLineTeam ls.home inningsPlayed })

            let team name abbr logo isUs hitters pits : BoxTeam =
                { Name = name
                  Abbr = abbr
                  Logo = logo |> Option.bind blankToNone
                  IsUs = isUs
                  Color = (if isUs then None else opponentColor)
                  Batters = hitters |> List.map (toBatter logger)
                  Pitchers = pits }

            let notes =
                [ boxNote "2B" b.AdditionalBatting2B
                  boxNote "3B" b.AdditionalBatting3B
                  boxNote "HR" b.AdditionalBattingHR
                  boxNote "SF" b.AdditionalBattingSF
                  boxNote "GIDP" b.AdditionalBattingGIDP
                  boxNote "SB" b.AdditionalBaserunningSB
                  boxNote "CS" b.AdditionalBaserunningCS
                  boxNote "E" b.AdditionalFieldingError
                  boxNote "DP" b.AdditionalFieldingDPs
                  boxNote "PB" b.AdditionalFieldingPB
                  boxNote "WP" b.AdditionalPitchingWP
                  boxNote "HBP" b.AdditionalPitchingHBP
                  boxNote "BK" b.AdditionalPitchingBalk ]
                |> List.choose id

            return
                Some
                    { GameId = string gameId
                      Date = logDate b.GameInfo.Date
                      Location = b.GameInfo.Misc.Field
                      Round = b.GameInfo.Round
                      Umpires = b.GameInfo.Misc.Umpires
                      Scorer = b.GameInfo.Misc.Scorer
                      LineScore = lineScore
                      Away = team b.AwayTeam b.AwayTeamAbbr b.AwayTeamLogo awayIsUs (side "T") (pitchers "T")
                      Home = team b.HomeTeam b.HomeTeamAbbr b.HomeTeamLogo homeIsUs (side "B") (pitchers "B")
                      Notes = notes }
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
