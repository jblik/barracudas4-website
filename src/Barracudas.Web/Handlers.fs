module Barracudas.Web.Handlers

open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Giraffe.ViewEngine
open Barracudas.Web
open Barracudas.Web.Domain
open Barracudas.Web.EasyScore.Client
open Barracudas.Web.Views

let private client (ctx: HttpContext) = ctx.GetService<IEasyScoreClient>()
let private cfg (ctx: HttpContext) = ctx.GetService<Config.AppConfig>()
let private aboutContent (ctx: HttpContext) = ctx.GetService<Content.AboutContent>()

/// Render a full page using the shared layout.
let private renderPage (active: string) (title: string) (ctx: HttpContext) (content: XmlNode list) =
    htmlView (Layout.page (cfg ctx).LivePollSeconds active title content)

let about : HttpHandler =
    fun next ctx -> renderPage "about" "About Us" ctx (Pages.About.view (aboutContent ctx)) next ctx

/// Filter games for a schedule tab: upcoming (scheduled + live), past (final), or all.
let private filterGames (tab: string) (games: Game list) =
    match tab with
    | "past" -> games |> List.filter (fun g -> g.Status = Final)
    | "all" -> games
    | _ -> games |> List.filter (fun g -> g.Status = Scheduled || g.Status = Live)

let schedule : HttpHandler =
    fun next ctx ->
        task {
            let! games = (client ctx).GetSchedule (cfg ctx).Season
            return! renderPage "schedule" "Schedule" ctx (Pages.Schedule.view "upcoming" (filterGames "upcoming" games)) next ctx
        }

let schedulePartial : HttpHandler =
    fun next ctx ->
        task {
            let tab = match ctx.TryGetQueryStringValue "tab" with Some t -> t | None -> "upcoming"
            let! games = (client ctx).GetSchedule (cfg ctx).Season
            return! htmlView (Pages.Schedule.content tab (filterGames tab games)) next ctx
        }

let standings : HttpHandler =
    fun next ctx ->
        task {
            let! table = (client ctx).GetStandings()
            let! stats = (client ctx).GetTeamStats()
            return! renderPage "standings" "Standings & Stats" ctx (Pages.Standings.view table stats) next ctx
        }

let players : HttpHandler =
    fun next ctx ->
        task {
            let sort = ctx.TryGetQueryStringValue "sort"
            let desc = ctx.TryGetQueryStringValue "dir" = Some "desc"
            let cols = Pages.Players.parseCols (ctx.TryGetQueryStringValue "cols")
            let! ps = (client ctx).GetPlayers()
            return! renderPage "players" "Players" ctx (Pages.Players.listView cols sort desc ps) next ctx
        }

let playersPartial : HttpHandler =
    fun next ctx ->
        task {
            let sort = ctx.TryGetQueryStringValue "sort"
            let desc = ctx.TryGetQueryStringValue "dir" = Some "desc"
            let cols = Pages.Players.parseCols (ctx.TryGetQueryStringValue "cols")
            let menu = ctx.TryGetQueryStringValue "menu" = Some "open"
            let! ps = (client ctx).GetPlayers()
            return! htmlView (Pages.Players.rosterPanel cols sort desc menu ps) next ctx
        }

let player (id: string) : HttpHandler =
    fun next ctx ->
        task {
            let! p = (client ctx).GetPlayer id
            match p with
            | Some pl ->
                let! stats = (client ctx).GetPlayerStats id
                return! renderPage "players" pl.Name ctx (Pages.Players.detailView pl stats) next ctx
            | None -> return! (setStatusCode 404 >=> text "Player not found") next ctx
        }

let boxScore (id: string) : HttpHandler =
    fun next ctx ->
        task {
            let! bs = (client ctx).GetBoxScore id
            match bs with
            | Some b -> return! renderPage "schedule" "Box Score" ctx (Pages.BoxScore.view b) next ctx
            | None -> return! (setStatusCode 404 >=> text "Box score not found") next ctx
        }

/// HTMX partial polled by the live banner. Returns the scoreboard markup when a
/// game is in progress, otherwise empty. In Development, ?forceLive=1 forces a
/// demo banner so the feature can be verified without a real live game.
let live : HttpHandler =
    fun next ctx ->
        task {
            let! lg = (client ctx).GetLiveGame()
            let env = ctx.GetService<IWebHostEnvironment>()
            let forced =
                env.IsDevelopment()
                && (ctx.TryGetQueryStringValue "forceLive" = Some "1")
            match lg, forced with
            | Some g, _ -> return! htmlView (Components.liveBanner g) next ctx
            | None, true ->
                // Real completed game so the overlay iframe has data to show.
                let demo = { GameId = "19313"; AwayName = "Demo Opponent"; HomeName = "Zürich Barracudas 4" }
                return! htmlView (Components.liveBanner demo) next ctx
            | None, false -> return! htmlString "" next ctx
        }

let home : HttpHandler = schedule
