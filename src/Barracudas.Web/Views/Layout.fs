module Barracudas.Web.Views.Layout

open Giraffe.ViewEngine
open Barracudas.Web.Views.Components

/// Current page is gold, the others green — matching the schedule tabs.
let private navLink (href: string) (label: string) (active: bool) =
    let baseCls = "block px-3 py-2 text-sm font-semibold rounded-md transition-colors"
    let cls =
        if active then baseCls + " bg-barracuda-accent text-barracuda-dark"
        else baseCls + " bg-barracuda-light text-white hover:bg-barracuda-line"
    a [ _href href; _class cls ] [ str label ]

let private navLinks (active: string) : XmlNode list =
    [ navLink "/schedule" "Schedule" (active = "schedule")
      navLink "/standings" "Standings" (active = "standings")
      navLink "/players" "Players" (active = "players")
      navLink "/about" "About" (active = "about") ]

// Runs before paint: applies a saved theme override so there's no light/dark flash.
let private earlyThemeScript =
    "(function(){try{var t=localStorage.getItem('theme');if(t==='dark'||t==='light')document.documentElement.setAttribute('data-theme',t);}catch(e){}})();"

// Toggle logic: flips data-theme, persists it, and updates the slider knob/icon.
// With no override the effective theme follows the device (prefers-color-scheme).
let private themeToggleScript =
    "(function(){function eff(){var t=document.documentElement.getAttribute('data-theme');if(t==='dark'||t==='light')return t;return window.matchMedia('(prefers-color-scheme: dark)').matches?'dark':'light';}function paint(){var d=eff()==='dark';var k=document.getElementById('theme-knob'),s=document.getElementById('theme-sun'),m=document.getElementById('theme-moon');if(k)k.style.transform=d?'translateX(1.5rem)':'translateX(0.125rem)';if(s)s.style.display=d?'none':'inline';if(m)m.style.display=d?'inline':'none';}window.toggleTheme=function(){var n=eff()==='dark'?'light':'dark';document.documentElement.setAttribute('data-theme',n);try{localStorage.setItem('theme',n);}catch(e){}paint();};try{window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change',function(){if(!localStorage.getItem('theme'))paint();});}catch(e){}paint();})();"

/// Slider-style light/dark switch — knob shows ☀ in light, ☾ in dark.
let private themeToggle =
    button [ _id "theme-toggle"; _type "button"
             KeyValue("onclick", "toggleTheme()")
             KeyValue("aria-label", "Toggle light or dark mode")
             _title "Toggle light / dark mode"
             _class "relative inline-flex h-7 w-12 shrink-0 items-center rounded-full bg-barracuda-dark ring-1 ring-barracuda-accent/50 transition-colors hover:ring-barracuda-accent" ] [
        span [ _id "theme-knob"
               _class "inline-flex h-5 w-5 items-center justify-center rounded-full bg-barracuda-accent text-[0.7rem] leading-none text-barracuda-dark shadow transition-transform"
               _style "transform: translateX(0.125rem)" ] [
            span [ _id "theme-sun" ] [ rawText "&#9728;" ]
            span [ _id "theme-moon"; _style "display:none" ] [ rawText "&#9790;" ]
        ]
    ]

/// Full HTML document shell. `active` is the nav key for the current page;
/// `pollSeconds` drives how often the live banner refreshes.
let page (pollSeconds: int) (active: string) (pageTitle: string) (content: XmlNode list) : XmlNode =
    html [ _lang "en"; _class "h-full" ] [
        head [] [
            meta [ _charset "utf-8" ]
            meta [ _name "viewport"; _content "width=device-width, initial-scale=1" ]
            script [] [ rawText earlyThemeScript ]
            title [] [ str $"%s{pageTitle} · Zürich Barracudas" ]
            link [ _rel "icon"; _type "image/png"; _href "/img/barracudas-logo.png" ]
            // Bai Jamjuree — same typeface as swiss-baseball.ch.
            link [ _rel "preconnect"; _href "https://fonts.googleapis.com" ]
            link [ _rel "preconnect"; _href "https://fonts.gstatic.com"; KeyValue("crossorigin", "") ]
            link [ _rel "stylesheet"; _href "https://fonts.googleapis.com/css2?family=Bai+Jamjuree:ital,wght@0,300;0,400;0,500;0,600;0,700;1,400;1,600&display=swap" ]
            link [ _rel "stylesheet"; _href "/css/site.css" ]
            script [ _src "/js/htmx.min.js"; _defer ] []
        ]
        // Sticky footer: body is a min-height-screen flex column, <main> grows.
        body [ _class "flex min-h-screen flex-col bg-gradient-to-b from-page to-page-2 font-sans text-ink antialiased" ] [
            // Live banner mount — polls /live and swaps in a scoreboard when a game is on.
            div [ _id "live-banner"
                  _hxGet "/live"
                  _hxTrigger $"load, every %d{pollSeconds}s"
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

                    // Right cluster: desktop links, theme toggle (always), mobile menu.
                    div [ _class "flex items-center gap-2" ] [
                        // Desktop: inline links.
                        div [ _class "hidden items-center gap-1 md:flex" ] (navLinks active)

                        themeToggle

                        // Mobile: CSS-only hamburger via <details>.
                        details [ _class "relative md:hidden" ] [
                            summary [ _class "flex cursor-pointer items-center rounded-md p-2 text-barracuda-accent hover:bg-barracuda-light" ] [
                                span [ _class "text-2xl leading-none" ] [ rawText "&#9776;" ]
                            ]
                            div [ _class "absolute right-0 top-full z-40 mt-2 flex w-44 flex-col gap-1 rounded-lg border border-barracuda-accent/40 bg-barracuda p-2 shadow-xl shadow-black/40" ]
                                (navLinks active)
                        ]
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

            script [] [ rawText themeToggleScript ]
        ]
    ]
