module Barracudas.Web.EasyScore.Cache

open System
open System.Threading.Tasks
open Microsoft.Extensions.Caching.Memory
open Barracudas.Web.Domain
open Barracudas.Web.EasyScore.Client

// Per-resource cache TTLs: reference data is cached for minutes, live for seconds.
let private contentTtl = TimeSpan.FromMinutes 5.0
let private liveTtl = TimeSpan.FromSeconds 12.0

/// Caching decorator over an inner IEasyScoreClient. Keeps EasyScore request
/// volume low (important for the live banner polling) without changing callers.
type CachingEasyScoreClient(inner: IEasyScoreClient, cache: IMemoryCache) =
    let getOrAdd (key: string) (ttl: TimeSpan) (factory: unit -> Task<'a>) : Task<'a> =
        match cache.TryGetValue key with
        | true, (:? 'a as v) -> Task.FromResult v
        | _ ->
            task {
                let! value = factory ()
                cache.Set(key, value, ttl) |> ignore
                return value
            }

    interface IEasyScoreClient with
        member _.GetSchedule(season) =
            getOrAdd (sprintf "schedule:%d" season) contentTtl (fun () -> inner.GetSchedule season)
        member _.GetStandings() =
            getOrAdd "standings" contentTtl inner.GetStandings
        member _.GetTeamStats() =
            getOrAdd "teamstats" contentTtl inner.GetTeamStats
        member _.GetPlayers() =
            getOrAdd "players" contentTtl inner.GetPlayers
        member _.GetPlayer(id) =
            getOrAdd (sprintf "player:%s" id) contentTtl (fun () -> inner.GetPlayer id)
        member _.GetPlayerStats(id) =
            getOrAdd (sprintf "playerstats:%s" id) contentTtl (fun () -> inner.GetPlayerStats id)
        member _.GetLiveGame() =
            getOrAdd "live" liveTtl inner.GetLiveGame
