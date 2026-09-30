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
        | HttpError(status, path) -> sprintf "HTTP %d from %s" status path
        | NetworkError(path, msg) -> sprintf "network error calling %s: %s" path msg
        | DecodeError(path, msg) -> sprintf "could not decode %s: %s" path msg
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
        getJson http (sprintf "rounds?byLeague=1&lg=%d" leagueId)

/// Teams playing a given round.
type TeamsApi(http: HttpClient) =
    member _.ByRound(roundId: int, leagueId: int, year: int) : Async<Result<TeamDto list, EasyScoreError>> =
        getJson http (sprintf "teams?byRound=%d&lg=%d&yr=%d" roundId leagueId year)

/// Full game schedule of a round (all teams, results included once played).
type ScheduleApi(http: HttpClient) =
    member _.ByRound(year: int, leagueId: int, roundId: int) : Async<Result<GameDto list, EasyScoreError>> =
        getJson http (sprintf "schedule?yr=%d&lg=%d&rd=%d" year leagueId roundId)

/// Licensed players of the federation user (all teams; filter by Team name).
type PlayersApi(http: HttpClient) =
    member _.ByUser(userId: string) : Async<Result<PlayerDto list, EasyScoreError>> =
        getJson http (sprintf "players?uid=%s" (System.Uri.EscapeDataString userId))
