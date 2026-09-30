module Barracudas.Web.EasyScore.Client

open System
open System.Net.Http
open System.Threading.Tasks
open Barracudas.Web
open Barracudas.Web.Domain

/// Data access over EasyScore. One method per page concern.
type IEasyScoreClient =
    /// Games for the given season, ordered by date.
    abstract member GetSchedule: season: int -> Task<Game list>
    /// League standings (our team flagged via Standing.IsUs).
    abstract member GetStandings: unit -> Task<Standing list>
    /// Season team stat summary (labelled values).
    abstract member GetTeamStats: unit -> Task<TeamStat list>
    /// Roster with season batting stats.
    abstract member GetPlayers: unit -> Task<PlayerStat list>
    /// A single player by id.
    abstract member GetPlayer: id: string -> Task<PlayerStat option>
    /// Current in-progress game, if any.
    abstract member GetLiveGame: unit -> Task<LiveGame option>

/// Real EasyScore client. Endpoints/JSON shapes are unconfirmed until an API key
/// is obtained (support@easyscore.com) and verified against postman.easyscore.com,
/// so methods currently raise. Until then run with EasyScore:UseMock=true.
/// The HttpClient is pre-configured (base address + auth header) in Program.fs.
type EasyScoreClient(http: HttpClient, cfg: Config.AppConfig) =
    let notReady name : 'a =
        raise (NotImplementedException(
            sprintf "EasyScore.%s not wired yet — set EasyScore:UseMock=true. TODO: confirm endpoint against postman.easyscore.com" name))

    interface IEasyScoreClient with
        // TODO: confirm against postman.easyscore.com — GET /seasons/{season}/teams/{TeamId}/games
        member _.GetSchedule(_season) = notReady "GetSchedule"
        // TODO: confirm against postman.easyscore.com — GET /leagues/{League}/standings
        member _.GetStandings() = notReady "GetStandings"
        // TODO: confirm against postman.easyscore.com — GET /teams/{TeamId}/stats
        member _.GetTeamStats() = notReady "GetTeamStats"
        // TODO: confirm against postman.easyscore.com — GET /teams/{TeamId}/players
        member _.GetPlayers() = notReady "GetPlayers"
        // TODO: confirm against postman.easyscore.com — GET /players/{id}
        member _.GetPlayer(_id) = notReady "GetPlayer"
        // TODO: confirm against postman.easyscore.com — GET /teams/{TeamId}/live
        member _.GetLiveGame() = notReady "GetLiveGame"
