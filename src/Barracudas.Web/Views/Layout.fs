module Barracudas.Web.Views.Layout

open Giraffe.ViewEngine
open Barracudas.Web.Views.Components

let private navLink (href: string) (label: string) (active: bool) =
    let baseCls = "px-3 py-2 text-sm font-medium rounded-md transition-colors"
    let cls =
        if active then baseCls + " bg-slate-800 text-white"
        else baseCls + " text-slate-300 hover:bg-slate-800 hover:text-white"
    a [ _href href; _class cls ] [ str label ]

/// Full HTML document shell. `active` is the nav key for the current page;
/// `pollSeconds` drives how often the live banner refreshes.
let page (pollSeconds: int) (active: string) (pageTitle: string) (content: XmlNode list) : XmlNode =
    html [ _lang "en"; _class "h-full bg-slate-900" ] [
        head [] [
            meta [ _charset "utf-8" ]
            meta [ _name "viewport"; _content "width=device-width, initial-scale=1" ]
            title [] [ str (sprintf "%s · Zürich Barracudas" pageTitle) ]
            link [ _rel "stylesheet"; _href "/css/site.css" ]
            script [ _src "/js/htmx.min.js"; _defer ] []
        ]
        body [ _class "min-h-full text-slate-200 antialiased" ] [
            // Live banner mount — polls /live and swaps in a scoreboard when a game is on.
            div [ _id "live-banner"
                  _hxGet "/live"
                  _hxTrigger (sprintf "load, every %ds" pollSeconds)
                  _hxSwap "innerHTML" ] []

            nav [ _class "border-b border-slate-800 bg-slate-950" ] [
                div [ _class "mx-auto flex max-w-5xl items-center gap-1 px-4 py-3" ] [
                    a [ _href "/"; _class "mr-4 flex items-center gap-2" ] [
                        span [ _class "text-lg font-extrabold tracking-tight text-barracuda-accent" ] [ str "Zürich Barracudas" ]
                        span [ _class "rounded bg-barracuda px-1.5 py-0.5 text-xs font-semibold text-white" ] [ str "1. Liga" ]
                    ]
                    navLink "/schedule" "Schedule" (active = "schedule")
                    navLink "/standings" "Standings" (active = "standings")
                    navLink "/players" "Players" (active = "players")
                    navLink "/about" "About" (active = "about")
                ]
            ]

            main [ _class "mx-auto max-w-5xl px-4 py-10" ] content

            footer [ _class "mt-16 border-t border-slate-800" ] [
                div [ _class "mx-auto max-w-5xl px-4 py-6 text-sm text-slate-500" ] [
                    str "Zürich Barracudas · 1. Liga · "
                    span [] [ str (string System.DateTime.Now.Year) ]
                    str " — data via EasyScore"
                ]
            ]
        ]
    ]
