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
open Barracudas.Web.EasyScore.SwissBaseball

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

    // Live data from the public swiss-baseball.ch league feed, behind the caching decorator.
    builder.Services.AddSingleton<IEasyScoreClient>(fun sp ->
        let cache = sp.GetRequiredService<IMemoryCache>()
        let http = sp.GetRequiredService<IHttpClientFactory>().CreateClient()
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; BarracudasWeb/1.0)")
        let inner = SwissBaseballClient(http, cfg) :> IEasyScoreClient
        CachingEasyScoreClient(inner, cache) :> IEasyScoreClient)
    |> ignore

    let app = builder.Build()
    app.UseStaticFiles() |> ignore
    app.UseGiraffe Router.webApp
    app.Run()
    0
