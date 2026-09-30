module Barracudas.Web.Views.Layout

open Giraffe.ViewEngine
open Barracudas.Web.Views.Components

let private navLink (href: string) (label: string) (active: bool) =
    let baseCls = "block px-3 py-2 text-sm font-semibold rounded-md transition-colors"
    let cls =
        if active then baseCls + " bg-barracuda-accent text-barracuda-dark"
        else baseCls + " text-emerald-100/80 hover:bg-barracuda-light hover:text-white"
    a [ _href href; _class cls ] [ str label ]

let private navLinks (active: string) : XmlNode list =
    [ navLink "/schedule" "Schedule" (active = "schedule")
      navLink "/standings" "Standings" (active = "standings")
      navLink "/players" "Players" (active = "players")
      navLink "/about" "About" (active = "about") ]

/// Full HTML document shell. `active` is the nav key for the current page;
/// `pollSeconds` drives how often the live banner refreshes.
let page (pollSeconds: int) (active: string) (pageTitle: string) (content: XmlNode list) : XmlNode =
    html [ _lang "en"; _class "h-full" ] [
        head [] [
            meta [ _charset "utf-8" ]
            meta [ _name "viewport"; _content "width=device-width, initial-scale=1" ]
            title [] [ str (sprintf "%s · Zürich Barracudas" pageTitle) ]
            link [ _rel "icon"; _type "image/png"; _href "/img/barracudas-logo.png" ]
            link [ _rel "stylesheet"; _href "/css/site.css" ]
            script [ _src "/js/htmx.min.js"; _defer ] []
        ]
        // Sticky footer: body is a min-height-screen flex column, <main> grows.
        body [ _class "flex min-h-screen flex-col bg-gradient-to-b from-page to-page-2 text-ink antialiased" ] [
            // Live banner mount — polls /live and swaps in a scoreboard when a game is on.
            div [ _id "live-banner"
                  _hxGet "/live"
                  _hxTrigger (sprintf "load, every %ds" pollSeconds)
                  _hxSwap "innerHTML" ] []

            nav [ _class "border-b-2 border-barracuda-accent bg-barracuda shadow-lg shadow-black/30" ] [
                div [ _class "mx-auto flex max-w-5xl items-center justify-between gap-3 px-4 py-3" ] [
                    // Brand: logo always; wordmark appears once there's room; badge on desktop.
                    div [ _class "flex items-center gap-2.5" ] [
                        a [ _href "/"; _class "flex items-center gap-2.5" ] [
                            img [ _src "/img/barracudas-logo.png"; _alt "Zürich Barracudas logo"; _class "h-10 w-auto" ]
                            span [ _class "hidden text-lg font-black uppercase tracking-tight text-barracuda-accent sm:inline-block" ] [ str "Barracudas" ]
                        ]
                        span [ _class "hidden whitespace-nowrap rounded bg-barracuda-light px-1.5 py-0.5 text-xs font-bold uppercase tracking-wide text-barracuda-gold ring-1 ring-barracuda-accent/40 lg:inline-block" ] [ str "1. Liga" ]
                    ]

                    // Desktop: inline links.
                    div [ _class "hidden items-center gap-1 md:flex" ] (navLinks active)

                    // Mobile: CSS-only hamburger via <details> (no JS).
                    details [ _class "relative md:hidden" ] [
                        summary [ _class "flex cursor-pointer items-center rounded-md p-2 text-barracuda-accent hover:bg-barracuda-light" ] [
                            span [ _class "text-2xl leading-none" ] [ rawText "&#9776;" ]
                        ]
                        div [ _class "absolute right-0 top-full z-40 mt-2 flex w-44 flex-col gap-1 rounded-lg border border-barracuda-accent/40 bg-barracuda p-2 shadow-xl shadow-black/40" ]
                            (navLinks active)
                    ]
                ]
            ]

            main [ _class "mx-auto w-full max-w-5xl flex-1 px-4 py-10" ] content

            footer [ _class "border-t-2 border-barracuda-accent/60 bg-barracuda-dark" ] [
                div [ _class "mx-auto max-w-5xl px-4 py-6 text-sm text-emerald-200/60" ] [
                    span [ _class "font-bold text-barracuda-accent" ] [ str "Zürich Barracudas" ]
                    str " · 1. Liga · "
                    span [] [ str (string System.DateTime.Now.Year) ]
                    str " — data via EasyScore"
                ]
            ]
        ]
    ]
