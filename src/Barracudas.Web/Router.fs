module Barracudas.Web.Router

open Giraffe
open Barracudas.Web

let webApp : HttpHandler =
    choose [
        GET >=> choose [
            route "/" >=> Handlers.home
            route "/about" >=> Handlers.about
            route "/schedule" >=> Handlers.schedule
            route "/schedule/partial" >=> Handlers.schedulePartial
            route "/standings" >=> Handlers.standings
            route "/players" >=> Handlers.players
            routef "/players/%s" Handlers.player
            route "/live" >=> Handlers.live
            route "/healthz" >=> text "ok"
        ]
        setStatusCode 404 >=> text "Not found"
    ]
