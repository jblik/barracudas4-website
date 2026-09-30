module Barracudas.Web.EasyScore.Api

open System.Net.Http
open System.Text.Json
open System.Text.Json.Serialization
open FsToolkit.ErrorHandling
open Barracudas.Web.EasyScore.Dto

/// Everything that can go wrong between the EasyScore API and our domain types.
type EasyScoreError =
    | HttpError of status: int * path: string
    | NetworkError of path: string * message: string
    | DecodeError of path: string * message: string
    | ConvertError of message: string

module EasyScoreError =
    let describe =
        function
        | HttpError(status, path) -> $"HTTP %d{status} from %s{path}"
        | NetworkError(path, msg) -> $"network error calling %s{path}: %s{msg}"
        | DecodeError(path, msg) -> $"could not decode %s{path}: %s{msg}"
        | ConvertError msg -> msg

let private jsonOptions =
    let o = JsonSerializerOptions(PropertyNameCaseInsensitive = true)
    JsonFSharpOptions
        .Default()
        .WithSkippableOptionFields(SkippableOptionFields.Always, deserializeNullAsNone = true)
        .AddToJsonSerializerOptions o
    o

let private decode<'T> (path: string) (body: string) : Result<'T, EasyScoreError> =
    try
        Ok(JsonSerializer.Deserialize<'T>(body, jsonOptions))
    with ex ->
        Error(DecodeError(path, ex.Message))

/// GET {BaseAddress}/{path} decoded as 'T. The HttpClient carries the base
/// address and x-api-key header (configured in Program from AppConfig).
let getJson<'T> (http: HttpClient) (path: string) : Async<Result<'T, EasyScoreError>> =
    asyncResult {
        try
            use! resp = http.GetAsync path |> Async.AwaitTask
            do! Result.requireTrue (HttpError(int resp.StatusCode, path)) resp.IsSuccessStatusCode
            let! body = resp.Content.ReadAsStringAsync() |> Async.AwaitTask
            return! decode<'T> path body
        with ex ->
            return! Error(NetworkError(path, ex.Message))
    }

/// Rounds (groups) of a league, e.g. Ost/Central/West of 1. Liga.
type RoundsApi(http: HttpClient) =
    member _.ByLeague(leagueId: int) : Async<Result<RoundDto list, EasyScoreError>> =
        getJson http $"rounds?byLeague=1&lg=%d{leagueId}"

/// Teams playing a given round.
type TeamsApi(http: HttpClient) =
    member _.ByRound(roundId: int, leagueId: int, year: int) : Async<Result<TeamDto list, EasyScoreError>> =
        getJson http $"teams?byRound=%d{roundId}&lg=%d{leagueId}&yr=%d{year}"

/// Full game schedule of a round (all teams, results included once played).
type ScheduleApi(http: HttpClient) =
    member _.ByRound(year: int, leagueId: int, roundId: int) : Async<Result<GameDto list, EasyScoreError>> =
        getJson http $"schedule?yr=%d{year}&lg=%d{leagueId}&rd=%d{roundId}"

/// Licensed players of the federation user (all teams; filter by Team name).
type PlayersApi(http: HttpClient) =
    member _.ByUser(userId: string) : Async<Result<PlayerDto list, EasyScoreError>> =
        getJson http $"players?uid=%s{System.Uri.EscapeDataString userId}"

/// The three stat categories the /stats endpoint serves (its `cat` parameter).
type StatCategory =
    | Offense
    | Fielding
    | Pitching

module StatCategory =
    let toQuery =
        function
        | Offense -> "off"
        | Fielding -> "fld"
        | Pitching -> "pit"

/// Per-player season statistics of a round (one row per player with stats).
type StatsApi(http: HttpClient) =
    let path (cat: StatCategory) (year: int) (leagueId: int) (roundId: int) =
        $"stats?yr=%d{year}&leagueID=%d{leagueId}&round=%d{roundId}&cat=%s{StatCategory.toQuery cat}"

    member _.Offense(year, leagueId, roundId) : Async<Result<OffenseStatsDto list, EasyScoreError>> =
        getJson http (path Offense year leagueId roundId)

    member _.Fielding(year, leagueId, roundId) : Async<Result<FieldingStatsDto list, EasyScoreError>> =
        getJson http (path Fielding year leagueId roundId)

    member _.Pitching(year, leagueId, roundId) : Async<Result<PitchingStatsDto list, EasyScoreError>> =
        getJson http (path Pitching year leagueId roundId)
