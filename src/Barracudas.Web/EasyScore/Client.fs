module Barracudas.Web.EasyScore.Client

open System.Net.Http
open System.Threading.Tasks
open Microsoft.Extensions.Logging
open FsToolkit.ErrorHandling
open Barracudas.Web
open Barracudas.Web.Domain
open Barracudas.Web.EasyScore.Api

/// Result-level data access over EasyScore. The caching decorator sits on
/// this interface so that failed fetches are never cached; only the outermost
/// DegradingEasyScoreClient turns errors into empty data.
type IEasyScoreSource =
    abstract member GetSchedule: season: int -> Task<Result<Game list, EasyScoreError>>
    abstract member GetStandings: unit -> Task<Result<Standing list, EasyScoreError>>
    abstract member GetTeamStats: unit -> Task<Result<TeamStat list, EasyScoreError>>
    abstract member GetPlayers: unit -> Task<Result<Player list, EasyScoreError>>
    abstract member GetPlayerStats: id: string -> Task<Result<PlayerStats, EasyScoreError>>
    abstract member GetBoxScore: gameId: string -> Task<Result<BoxScore option, EasyScoreError>>
    abstract member GetLiveGame: unit -> Task<Result<LiveGame option, EasyScoreError>>

/// Data access for the handlers. One method per page concern; errors already
/// degraded to empty data.
type IEasyScoreClient =
    /// Our games for the given season, ordered by date.
    abstract member GetSchedule: season: int -> Task<Game list>
    /// League standings (our team flagged via Standing.IsUs).
    abstract member GetStandings: unit -> Task<Standing list>
    /// Season team stat summary (labelled values).
    abstract member GetTeamStats: unit -> Task<TeamStat list>
    /// Team roster.
    abstract member GetPlayers: unit -> Task<Player list>
    /// A single roster player by id.
    abstract member GetPlayer: id: string -> Task<Player option>
    /// Season batting/fielding/pitching lines of a player.
    abstract member GetPlayerStats: id: string -> Task<PlayerStats>
    /// A completed game's box score (None when no box score is available).
    abstract member GetBoxScore: gameId: string -> Task<BoxScore option>
    /// Current in-progress game, if any.
    abstract member GetLiveGame: unit -> Task<LiveGame option>

/// IEasyScoreSource over the EasyScore v2 REST API. Composes the per-resource
/// fetch classes (Api) with the DTO→domain converters (Convert).
type EasyScoreApiClient(http: HttpClient, cfg: Config.AppConfig) =
    let roundsApi = RoundsApi http
    let teamsApi = TeamsApi http
    let scheduleApi = ScheduleApi http
    let playersApi = PlayersApi http
    let statsApi = StatsApi http
    let gamesApi = GamesApi http

    /// Our round id (1. Liga Ost), resolved from the league's rounds.
    let roundId () =
        asyncResult {
            let! rounds = roundsApi.ByLeague cfg.LeagueId
            let! round = Convert.findRound cfg.RoundFilter rounds
            return round.ID
        }

    /// All games of our round (whole league, every team).
    let leagueGames (season: int) =
        asyncResult {
            let! rd = roundId ()
            return! scheduleApi.ByRound(season, cfg.LeagueId, rd)
        }

    let ourGames (season: int) =
        asyncResult {
            let! games = leagueGames season
            return! Convert.toGames cfg.TeamId games
        }

    let standings () =
        asyncResult {
            let! rd = roundId ()
            let! teams = teamsApi.ByRound(rd, cfg.LeagueId, cfg.Season)
            let! games = scheduleApi.ByRound(cfg.Season, cfg.LeagueId, rd)
            return! Convert.toStandings cfg.TeamId teams games
        }

    /// Round-wide offensive stats (one row per player with plate appearances).
    let offenseStats () =
        asyncResult {
            let! rd = roundId ()
            return! statsApi.Offense(cfg.Season, cfg.LeagueId, rd)
        }

    let roster () =
        asyncResult {
            let! players = playersApi.ByUser cfg.RequestUserId
            let! offense = offenseStats ()
            return! Convert.toRoster cfg.ActiveRoster players offense
        }

    let playerStats (id: string) =
        asyncResult {
            let! playerId =
                match System.Int32.TryParse id with
                | true, v -> Ok v
                | _ -> Error(ConvertError $"invalid player id '%s{id}'")
            let! rd = roundId ()
            let! offense = statsApi.Offense(cfg.Season, cfg.LeagueId, rd)
            let! fielding = statsApi.Fielding(cfg.Season, cfg.LeagueId, rd)
            let! pitching = statsApi.Pitching(cfg.Season, cfg.LeagueId, rd)
            let! battingLog = statsApi.OffenseLog(cfg.Season, cfg.LeagueId, rd, playerId)
            let! fieldingLog = statsApi.FieldingLog(cfg.Season, cfg.LeagueId, rd, playerId)
            let! pitchingLog = statsApi.PitchingLog(cfg.Season, cfg.LeagueId, rd, playerId)
            return! Convert.toPlayerStats playerId offense fielding pitching battingLog fieldingLog pitchingLog
        }

    /// A completed game's box score: linescore from /games, lines from /stats?box.
    let boxScore (id: string) =
        asyncResult {
            let! gameId =
                match System.Int32.TryParse id with
                | true, v -> Ok v
                | _ -> Error(ConvertError $"invalid game id '%s{id}'")
            let! detail = gamesApi.ById gameId
            let! box = statsApi.BoxScore gameId
            return! Convert.toBoxScore cfg.TeamId gameId detail box
        }

    let toTask (work: Async<Result<'a, EasyScoreError>>) = Async.StartImmediateAsTask work

    interface IEasyScoreSource with
        member _.GetSchedule season = toTask (ourGames season)

        member _.GetStandings() = toTask (standings ())

        member _.GetTeamStats() =
            toTask (asyncResult {
                let! table = standings ()
                let! games = ourGames cfg.Season
                return! Convert.toTeamStats table games
            })

        member _.GetPlayers() = toTask (roster ())

        member _.GetPlayerStats id = toTask (playerStats id)

        member _.GetBoxScore id = toTask (boxScore id)

        member _.GetLiveGame() =
            toTask (asyncResult {
                let! games = leagueGames cfg.Season
                return! Convert.toLiveGame cfg.TeamId games
            })

/// IEasyScoreClient over an IEasyScoreSource: logs failures and degrades them
/// to empty data so pages render. Sits above the cache, so failed responses
/// reach the user as empty pages but are never cached.
type DegradingEasyScoreClient(source: IEasyScoreSource, logger: ILogger<DegradingEasyScoreClient>) =
    let orEmpty (name: string) (empty: 'a) (result: Task<Result<'a, EasyScoreError>>) : Task<'a> =
        task {
            match! result with
            | Ok v -> return v
            | Error e ->
                logger.LogWarning("EasyScore {Operation} failed: {Error}", name, EasyScoreError.describe e)
                return empty
        }

    interface IEasyScoreClient with
        member _.GetSchedule season = orEmpty "GetSchedule" [] (source.GetSchedule season)

        member _.GetStandings() = orEmpty "GetStandings" [] (source.GetStandings())

        member _.GetTeamStats() = orEmpty "GetTeamStats" [] (source.GetTeamStats())

        member _.GetPlayers() = orEmpty "GetPlayers" [] (source.GetPlayers())

        member this.GetPlayer id =
            // Lookup into the roster (cached one layer down).
            task {
                let! players = (this :> IEasyScoreClient).GetPlayers()
                return players |> List.tryFind (fun p -> p.Id = id)
            }

        member _.GetPlayerStats id =
            let empty =
                { Batting = None
                  Fielding = None
                  Pitching = None
                  BattingLog = []
                  FieldingLog = []
                  PitchingLog = [] }
            orEmpty "GetPlayerStats" empty (source.GetPlayerStats id)

        member _.GetBoxScore id = orEmpty "GetBoxScore" None (source.GetBoxScore id)

        member _.GetLiveGame() = orEmpty "GetLiveGame" None (source.GetLiveGame())
