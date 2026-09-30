module Barracudas.Web.EasyScore.Cache

open System
open System.Threading.Tasks
open Microsoft.Extensions.Caching.Memory
open Barracudas.Web
open Barracudas.Web.Domain
open Barracudas.Web.EasyScore.Api
open Barracudas.Web.EasyScore.Client

/// All EasyScore caching lives in this module: one decorator over
/// IEasyScoreSource, one TTL policy (Policy below). Only successful fetches
/// are cached — errors pass through to the degrading client untouched.
/// EasyScore data only changes on game days, so everything is cached until
/// the next one; on a game day itself only the live banner keeps polling the
/// API, and player data drops to a short TTL while a game is underway.

/// Wrapper around cached values: a bare F# None boxes to null, which
/// IMemoryCache.TryGetValue cannot distinguish from a cache miss.
type private CacheEntry<'a> = { Value: 'a }

module private Swiss =
    let private zone =
        try TimeZoneInfo.FindSystemTimeZoneById "Europe/Zurich"
        with _ -> TimeZoneInfo.Local

    /// Game dates are Swiss wall-clock, so the policy clock must be too
    /// (the production container runs on UTC).
    let now () = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone)

/// How long a freshly fetched value stays valid; Until is Swiss wall-clock.
type Freshness =
    | For of TimeSpan
    | Until of DateTime

/// The cache TTL rules, all in one place. Each rule derives its expiry from
/// our schedule (the source of game days) and the current Swiss time.
module Policy =
    /// Used when the schedule itself is unavailable, so no game-day expiry
    /// can be computed for an otherwise successful fetch.
    let fallbackTtl = TimeSpan.FromMinutes 5.0
    /// Live banner refresh on a game day (pages poll /live every ~25 s).
    let liveTtl = TimeSpan.FromSeconds 12.0
    /// Player data while a game is underway.
    let livePlayersTtl = TimeSpan.FromMinutes 2.0
    /// Nothing left on the schedule (off-season).
    let offSeasonTtl = TimeSpan.FromHours 24.0

    let private gameDays (games: Game list) =
        games |> List.map (fun g -> g.Date.Date) |> List.distinct |> List.sort

    let private isGameDay games (now: DateTime) =
        gameDays games |> List.contains now.Date

    let private nextGameDay games (now: DateTime) =
        gameDays games |> List.tryFind (fun d -> d > now.Date)

    /// First pitch of today's slate has passed: a game is (or may be) live,
    /// including between doubleheader games. Resets at midnight.
    let private underway (games: Game list) (now: DateTime) =
        games |> List.exists (fun g -> g.Date.Date = now.Date && g.Date <= now)

    /// Schedule, standings, team stats: frozen until the next game day; on a
    /// game day frozen for the rest of it (results show the next morning).
    let content games (now: DateTime) =
        if isGameDay games now then Until(now.Date.AddDays 1.0)
        else
            match nextGameDay games now with
            | Some day -> Until day
            | None -> For offSeasonTtl

    /// Player data: short TTL while a game is underway; otherwise valid up to
    /// the next first pitch, so a starting game invalidates it promptly.
    let players games (now: DateTime) =
        if underway games now then
            For livePlayersTtl
        else
            match games |> List.map _.Date |> List.filter (fun d -> d > now) |> List.sort |> List.tryHead with
            | Some firstPitch -> Until firstPitch
            | None -> For offSeasonTtl

    /// Live banner: polls the API all game day, parked until the next one otherwise.
    let live games (now: DateTime) =
        if isGameDay games now then For liveTtl
        else
            match nextGameDay games now with
            | Some day -> Until day
            | None -> For offSeasonTtl

/// Caching decorator over an inner IEasyScoreSource. Keeps EasyScore request
/// volume low (at most a handful of fetches per day outside game days)
/// without changing callers. Failed fetches are never cached.
type CachingEasyScoreSource(inner: IEasyScoreSource, cfg: Config.AppConfig, cache: IMemoryCache) =
    let getOrAdd
        (key: string)
        (freshness: 'a -> Freshness)
        (factory: unit -> Task<Result<'a, EasyScoreError>>)
        : Task<Result<'a, EasyScoreError>> =
        match cache.TryGetValue key with
        | true, (:? CacheEntry<'a> as e) -> Task.FromResult(Ok e.Value)
        | _ ->
            task {
                match! factory () with
                | Error e -> return Error e
                | Ok value ->
                    let ttl =
                        match freshness value with
                        | For t -> t
                        | Until instant -> max (instant - Swiss.now ()) (TimeSpan.FromSeconds 30.0)
                    cache.Set(key, { Value = value }, ttl) |> ignore
                    return Ok value
            }

    /// Our schedule is both cached content and the source of game days for
    /// every other TTL, so its freshness is derived from its own value.
    let schedule (season: int) =
        getOrAdd
            $"schedule:%d{season}"
            (fun games -> Policy.content games (Swiss.now ()))
            (fun () -> inner.GetSchedule season)

    /// Cache `key` under `policy`, deriving game days from the schedule.
    let policied
        (key: string)
        (policy: Game list -> DateTime -> Freshness)
        (factory: unit -> Task<Result<'a, EasyScoreError>>)
        : Task<Result<'a, EasyScoreError>> =
        task {
            let! sched = schedule cfg.Season
            let freshness =
                match sched with
                | Ok games -> fun _ -> policy games (Swiss.now ())
                | Error _ -> fun _ -> For Policy.fallbackTtl
            return! getOrAdd key freshness factory
        }

    interface IEasyScoreSource with
        member _.GetSchedule season = schedule season

        member _.GetStandings() = policied "standings" Policy.content inner.GetStandings

        member _.GetTeamStats() = policied "teamstats" Policy.content inner.GetTeamStats

        member _.GetPlayers() = policied "players" Policy.players inner.GetPlayers

        member _.GetPlayerStats logger id =
            policied $"playerstats:%s{id}" Policy.players (fun () -> inner.GetPlayerStats logger id)

        // A completed game's box score never changes; cache it like other content.
        member _.GetBoxScore id =
            policied $"boxscore:%s{id}" Policy.content (fun () -> inner.GetBoxScore id)

        member _.GetLiveGame() = policied "live" Policy.live inner.GetLiveGame
