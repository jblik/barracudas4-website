module Barracudas.Web.Program

open System
open System.IO
open System.Net.Http
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Caching.Memory
open Microsoft.Extensions.Logging
open Giraffe
open Barracudas.Web
open Barracudas.Web.EasyScore.Client
open Barracudas.Web.EasyScore.Cache

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)
    let cfg = Config.load builder.Configuration

    builder.Services.AddSingleton<Config.AppConfig>(cfg) |> ignore
    builder.Services.AddMemoryCache() |> ignore
    builder.Services.AddHttpClient() |> ignore
    builder.Services.AddGiraffe() |> ignore

    let aboutPath = Path.Combine(builder.Environment.ContentRootPath, "content", "about.json")
    builder.Services.AddSingleton<Content.AboutContent>(Content.loadAbout aboutPath) |> ignore

    // Live data from the EasyScore v2 API, behind the caching decorator.
    // The API key arrives via configuration (EasyScore__ApiKey env var in prod).
    builder.Services.AddSingleton<IEasyScoreClient>(fun sp ->
        let cache = sp.GetRequiredService<IMemoryCache>()
        let http = sp.GetRequiredService<IHttpClientFactory>().CreateClient()
        http.BaseAddress <- Uri(cfg.BaseUrl.TrimEnd '/' + "/")
        http.DefaultRequestHeaders.Add("x-api-key", cfg.ApiKey)
        let logger = sp.GetRequiredService<ILogger<EasyScoreApiClient>>()
        let inner = EasyScoreApiClient(http, cfg, logger) :> IEasyScoreClient
        CachingEasyScoreClient(inner, cfg, cache) :> IEasyScoreClient)
    |> ignore

    let app = builder.Build()
    app.UseStaticFiles() |> ignore
    app.UseGiraffe Router.webApp
    app.Run()
    0
