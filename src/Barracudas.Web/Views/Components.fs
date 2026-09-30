module Barracudas.Web.Views.Components

open System
open Giraffe.ViewEngine
open Barracudas.Web.Domain

// --- HTMX attribute helpers (Giraffe.ViewEngine has no built-ins for hx-*) ---
let _hxGet (v: string) = KeyValue("hx-get", v)
let _hxTrigger (v: string) = KeyValue("hx-trigger", v)
let _hxSwap (v: string) = KeyValue("hx-swap", v)
let _hxTarget (v: string) = KeyValue("hx-target", v)
let _hxPushUrl (v: string) = KeyValue("hx-push-url", v)

/// Standard page heading.
let pageHeader (title: string) (subtitle: string) =
    header [ _class "mb-8" ] [
        h1 [ _class "text-3xl font-bold text-white" ] [ str title ]
        if subtitle <> "" then
            p [ _class "mt-1 text-slate-400" ] [ str subtitle ]
    ]

/// A labelled stat card (used on standings / team stats).
let statCard (s: TeamStat) =
    div [ _class "rounded-lg bg-slate-800 p-4 ring-1 ring-slate-700" ] [
        div [ _class "text-2xl font-bold text-barracuda-accent" ] [ str s.Value ]
        div [ _class "mt-1 text-sm uppercase tracking-wide text-slate-400" ] [ str s.Label ]
    ]

let private statusBadge (status: GameStatus) =
    let cls, label =
        match status with
        | Live -> "bg-red-600 text-white animate-pulse", "LIVE"
        | Final -> "bg-slate-600 text-slate-100", "Final"
        | Scheduled -> "bg-barracuda text-white", "Upcoming"
    span [ _class (sprintf "rounded px-2 py-0.5 text-xs font-semibold %s" cls) ] [ str label ]

let private scoreText (g: Game) =
    match g.OurScore, g.OpponentScore with
    | Some us, Some them -> sprintf "%d – %d" us them
    | _ -> "—"

/// One row in a schedule list.
let gameRow (g: Game) =
    let homeAway = if g.IsHome then "vs" else "@"
    tr [ _class "border-b border-slate-700 hover:bg-slate-800/50" ] [
        td [ _class "py-3 pr-4 text-slate-400 whitespace-nowrap" ] [ str (g.Date.ToString "ddd, MMM d") ]
        td [ _class "py-3 pr-4 font-medium text-white" ] [ str (sprintf "%s %s" homeAway g.Opponent) ]
        td [ _class "py-3 pr-4 text-slate-400" ] [ str g.Location ]
        td [ _class "py-3 pr-4 font-semibold text-white whitespace-nowrap" ] [ str (scoreText g) ]
        td [ _class "py-3" ] [ statusBadge g.Status ]
    ]

/// The live scoreboard banner fragment returned by /live.
let liveBanner (lg: LiveGame) =
    let half = if lg.IsTop then "Top" else "Bot"
    let matchup =
        if lg.IsHome then sprintf "Barracudas vs %s" lg.Opponent
        else sprintf "Barracudas @ %s" lg.Opponent
    div [ _class "bg-red-700 text-white" ] [
        div [ _class "mx-auto flex max-w-5xl items-center gap-4 px-4 py-2 text-sm" ] [
            span [ _class "rounded bg-white/20 px-2 py-0.5 text-xs font-bold tracking-wide animate-pulse" ] [ str "● LIVE" ]
            span [ _class "font-semibold" ] [ str matchup ]
            span [ _class "ml-auto font-mono text-lg font-bold" ] [
                str (sprintf "%d – %d" lg.OurScore lg.OpponentScore)
            ]
            span [ _class "text-white/80" ] [ str (sprintf "%s %d · %d out" half lg.Inning lg.Outs) ]
        ]
    ]
