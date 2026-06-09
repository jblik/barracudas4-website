module Barracudas.Web.Program

open System
open System.IO
open System.Net.Http
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Caching.Memory
open Giraffe
open Barracudas.Web
open Barracudas.Web.EasyScore.Client
open Barracudas.Web.EasyScore.Cache
open Barracudas.Web.EasyScore.Mock

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

    // EasyScore client: mock or real, always behind the caching decorator.
    builder.Services.AddSingleton<IEasyScoreClient>(fun sp ->
        let cache = sp.GetRequiredService<IMemoryCache>()
        let inner : IEasyScoreClient =
            if cfg.UseMock then
                MockEasyScoreClient() :> IEasyScoreClient
            else
                let http = sp.GetRequiredService<IHttpClientFactory>().CreateClient()
                http.BaseAddress <- Uri(cfg.BaseUrl)
                if cfg.ApiKey <> "" then
                    http.DefaultRequestHeaders.Add("X-API-Key", cfg.ApiKey)
                EasyScoreClient(http, cfg) :> IEasyScoreClient
        CachingEasyScoreClient(inner, cache) :> IEasyScoreClient)
    |> ignore

    let app = builder.Build()
    app.UseStaticFiles() |> ignore
    app.UseGiraffe Router.webApp
    app.Run()
    0
