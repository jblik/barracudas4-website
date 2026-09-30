module Barracudas.Web.EasyScore.Cache

open System
open System.Threading.Tasks
open Microsoft.Extensions.Caching.Memory
open Barracudas.Web
open Barracudas.Web.Domain
open Barracudas.Web.EasyScore.Client

/// All EasyScore caching lives in this module: one decorator over
/// IEasyScoreClient, one TTL policy (Policy below). EasyScore data only
/// changes on game days, so everything is cached until the next one; on a
/// game day itself only the live banner keeps polling the API, and player
/// data drops to a short TTL while a game is underway.

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
    /// Failed fetches degrade to empty data — retry those soon.
    let errorTtl = TimeSpan.FromMinutes 5.0
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

/// Caching decorator over an inner IEasyScoreClient. Keeps EasyScore request
/// volume low (at most a handful of fetches per day outside game days)
/// without changing callers.
type CachingEasyScoreClient(inner: IEasyScoreClient, cfg: Config.AppConfig, cache: IMemoryCache) =
    let getOrAdd (key: string) (freshness: 'a -> Freshness) (factory: unit -> Task<'a>) : Task<'a> =
        match cache.TryGetValue key with
        | true, (:? CacheEntry<'a> as e) -> Task.FromResult e.Value
        | _ ->
            task {
                let! value = factory ()
                let ttl =
                    match freshness value with
                    | For t -> t
                    | Until instant -> max (instant - Swiss.now ()) (TimeSpan.FromSeconds 30.0)
                cache.Set(key, { Value = value }, ttl) |> ignore
                return value
            }

    /// Our schedule is both cached content and the source of game days for
    /// every other TTL, so its freshness is derived from its own value.
    let schedule (season: int) =
        getOrAdd
            $"schedule:%d{season}"
            (fun games ->
                if List.isEmpty games then For Policy.errorTtl
                else Policy.content games (Swiss.now ()))
            (fun () -> inner.GetSchedule season)

    /// Cache `key` under `policy`. `degraded` flags values that came from a
    /// failed fetch (the inner client degrades errors to empty data); those
    /// get the short error TTL instead of sticking around until game day.
    let policied (key: string) (policy: Game list -> DateTime -> Freshness) (degraded: 'a -> bool) (factory: unit -> Task<'a>) : Task<'a> =
        task {
            let! games = schedule cfg.Season
            return!
                getOrAdd
                    key
                    (fun value ->
                        if degraded value || List.isEmpty games then For Policy.errorTtl
                        else policy games (Swiss.now ()))
                    factory
        }

    let players () = policied "players" Policy.players List.isEmpty inner.GetPlayers

    interface IEasyScoreClient with
        member _.GetSchedule season = schedule season

        member _.GetStandings() =
            policied "standings" Policy.content List.isEmpty inner.GetStandings

        member _.GetTeamStats() =
            policied "teamstats" Policy.content List.isEmpty inner.GetTeamStats

        member _.GetPlayers() = players ()

        member _.GetPlayer id =
            // Lookup into the cached roster instead of a per-player fetch.
            task {
                let! ps = players ()
                return ps |> List.tryFind (fun p -> p.Id = id)
            }

        member _.GetPlayerStats id =
            policied
                $"playerstats:%s{id}"
                Policy.players
                (fun (s: PlayerStats) -> s.Batting.IsNone && s.Fielding.IsNone && s.Pitching.IsNone)
                (fun () -> inner.GetPlayerStats id)

        member _.GetLiveGame() =
            // None is the normal pre-game answer, not a degraded value.
            policied "live" Policy.live (fun _ -> false) inner.GetLiveGame
