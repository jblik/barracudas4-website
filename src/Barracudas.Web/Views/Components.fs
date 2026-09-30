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

/// Standard page heading with a gold accent bar.
let pageHeader (title: string) (subtitle: string) =
    header [ _class "mb-8 border-l-4 border-barracuda-accent pl-4" ] [
        h1 [ _class "text-3xl font-black uppercase tracking-tight text-ink-strong" ] [ str title ]
        if subtitle <> "" then
            p [ _class "mt-1 font-medium text-accent-text" ] [ str subtitle ]
    ]

/// A labelled stat card (used on standings / team stats).
let statCard (s: TeamStat) =
    div [ _class "rounded-lg bg-card p-4 ring-1 ring-card-ring transition-colors hover:ring-barracuda-accent/70" ] [
        div [ _class "text-3xl font-black text-accent-text" ] [ str s.Value ]
        div [ _class "mt-1 text-sm font-semibold uppercase tracking-wide text-ink-muted" ] [ str s.Label ]
    ]

let private statusBadge (status: GameStatus) =
    let cls, label =
        match status with
        | Live -> "bg-red-600 text-white animate-pulse", "LIVE"
        | Final -> "bg-barracuda-light text-emerald-100 ring-1 ring-barracuda-line", "Final"
        | Scheduled -> "bg-barracuda-accent text-barracuda-dark", "Upcoming"
    span [ _class (sprintf "rounded px-2 py-0.5 text-xs font-bold uppercase tracking-wide %s" cls) ] [ str label ]

let private scoreText (g: Game) =
    match g.OurScore, g.OpponentScore with
    | Some us, Some them -> sprintf "%d – %d" us them
    | _ -> "—"

/// One row in a schedule list. Completed games link to their EasyScore box score.
let gameRow (g: Game) =
    let homeAway = if g.IsHome then "vs" else "@"
    let baseCls = "border-b border-line transition-colors hover:bg-row-hover"
    let rowAttrs =
        match g.BoxScoreUrl with
        | Some url ->
            [ _class (baseCls + " cursor-pointer")
              KeyValue("onclick", sprintf "window.open('%s','_blank','noopener')" url)
              _title "View box score on EasyScore" ]
        | None -> [ _class baseCls ]
    let score =
        match g.BoxScoreUrl with
        | Some _ -> sprintf "%s ↗" (scoreText g)
        | None -> scoreText g
    tr rowAttrs [
        td [ _class "py-3 pr-4 text-ink-muted whitespace-nowrap" ] [ str (g.Date.ToString "ddd, MMM d") ]
        td [ _class "py-3 pr-4 font-semibold text-ink-strong" ] [ str (sprintf "%s %s" homeAway g.Opponent) ]
        td [ _class "py-3 pr-4 text-ink-muted" ] [ str g.Location ]
        td [ _class "py-3 pr-4 font-bold text-accent-text whitespace-nowrap" ] [ str score ]
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
