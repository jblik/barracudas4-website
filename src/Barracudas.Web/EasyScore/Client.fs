module Barracudas.Web.EasyScore.Client

open System.Net.Http
open System.Threading.Tasks
open Microsoft.Extensions.Logging
open FsToolkit.ErrorHandling
open Barracudas.Web
open Barracudas.Web.Domain
open Barracudas.Web.EasyScore.Api

/// Data access over EasyScore. One method per page concern.
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
    /// Current in-progress game, if any.
    abstract member GetLiveGame: unit -> Task<LiveGame option>

/// IEasyScoreClient over the EasyScore v2 REST API. Composes the per-resource
/// fetch classes (Api) with the DTO→domain converters (Convert); any failure
/// is logged and surfaced as empty data so pages degrade gracefully.
type EasyScoreApiClient(http: HttpClient, cfg: Config.AppConfig, logger: ILogger<EasyScoreApiClient>) =
    let roundsApi = RoundsApi http
    let teamsApi = TeamsApi http
    let scheduleApi = ScheduleApi http
    let playersApi = PlayersApi http
    let statsApi = StatsApi http

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
            let! rd = roundId ()
            let! offense = statsApi.Offense(cfg.Season, cfg.LeagueId, rd)
            let! fielding = statsApi.Fielding(cfg.Season, cfg.LeagueId, rd)
            let! pitching = statsApi.Pitching(cfg.Season, cfg.LeagueId, rd)
            let! playerId =
                match System.Int32.TryParse id with
                | true, v -> Ok v
                | _ -> Error(ConvertError $"invalid player id '%s{id}'")
            return! Convert.toPlayerStats playerId offense fielding pitching
        }

    /// Run a pipeline; on error, log it and fall back to empty data.
    let run (name: string) (empty: 'a) (work: Async<Result<'a, EasyScoreError>>) : Task<'a> =
        task {
            match! Async.StartImmediateAsTask work with
            | Ok v -> return v
            | Error e ->
                logger.LogWarning("EasyScore {Operation} failed: {Error}", name, EasyScoreError.describe e)
                return empty
        }

    interface IEasyScoreClient with
        member _.GetSchedule season = run "GetSchedule" [] (ourGames season)

        member _.GetStandings() = run "GetStandings" [] (standings ())

        member _.GetTeamStats() =
            run "GetTeamStats" [] (asyncResult {
                let! table = standings ()
                let! games = ourGames cfg.Season
                return! Convert.toTeamStats table games
            })

        member _.GetPlayers() = run "GetPlayers" [] (roster ())

        member _.GetPlayer id =
            run "GetPlayer" None (asyncResult {
                let! players = roster ()
                return players |> List.tryFind (fun p -> p.Id = id)
            })

        member _.GetPlayerStats id =
            run "GetPlayerStats" { Batting = None; Fielding = None; Pitching = None } (playerStats id)

        member _.GetLiveGame() =
            run "GetLiveGame" None (asyncResult {
                let! games = leagueGames cfg.Season
                return! Convert.toLiveGame cfg.TeamId games
            })
