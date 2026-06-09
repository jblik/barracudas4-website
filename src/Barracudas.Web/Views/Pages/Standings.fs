module Barracudas.Web.Views.Pages.Standings

open System.Globalization
open Giraffe.ViewEngine
open Barracudas.Web.Domain
open Barracudas.Web.Views.Components

let private inv = CultureInfo.InvariantCulture

/// Baseball winning percentage: "1.000" but ".667" / ".000" (no leading zero).
let private fmtPct (p: float) =
    let s = p.ToString("0.000", inv)
    if s.StartsWith "0" then s.Substring 1 else s

/// Games behind: "0" for the leader, whole numbers without a decimal, else "0.5".
let private fmtGb (gb: float) =
    if gb = 0.0 then "0"
    elif gb % 1.0 = 0.0 then sprintf "%.0f" gb
    else gb.ToString("0.0", inv)

let private hcol extra label = th [ _class ("pb-2 " + extra) ] [ str label ]

let private streakBadge (streak: string) =
    let cls =
        if streak.StartsWith "W" then "bg-barracuda-light text-barracuda-gold ring-1 ring-barracuda-accent/40"
        else "bg-red-600/20 text-red-400 ring-1 ring-red-500/40"
    span [ _class (sprintf "inline-block rounded px-1.5 py-0.5 text-xs font-bold %s" cls) ] [ str streak ]

let private row (s: Standing) =
    let rowCls =
        if s.IsUs then "border-b border-line border-l-4 border-l-barracuda-accent bg-barracuda-accent/15 font-bold"
        else "border-b border-line transition-colors hover:bg-row-hover"
    let teamCls = if s.IsUs then "text-accent-text" else "text-ink-strong"
    tr [ _class rowCls ] [
        td [ _class "py-3 pr-4 text-ink-muted" ] [ str (sprintf "%d." s.Rank) ]
        td [ _class ("py-3 pr-4 font-semibold " + teamCls) ] [
            str s.Team
            span [ _class "ml-2 rounded bg-card px-1.5 py-0.5 align-middle text-[0.65rem] font-bold uppercase tracking-wide text-ink-muted ring-1 ring-card-ring" ] [ str s.Abbr ]
        ]
        td [ _class "py-3 pr-4 text-center text-ink-muted" ] [ str (string s.Games) ]
        td [ _class "py-3 pr-4 text-center" ] [ str (string s.Wins) ]
        td [ _class "py-3 pr-4 text-center" ] [ str (string s.Losses) ]
        td [ _class "py-3 pr-4 text-center font-semibold" ] [ str (fmtPct s.Pct) ]
        td [ _class "py-3 pr-4 text-center text-ink-muted" ] [ str (fmtGb s.GamesBehind) ]
        td [ _class "py-3 text-center" ] [ streakBadge s.Streak ]
    ]

let view (standings: Standing list) (teamStats: TeamStat list) : XmlNode list =
    [ pageHeader "Standings & Stats" "1. Liga Baseball Ost"
      div [ _class "overflow-x-auto" ] [
       table [ _class "w-full min-w-[40rem] text-left text-sm" ] [
          thead [ _class "border-b border-barracuda-accent/40 text-xs font-bold uppercase tracking-wider text-accent-text" ] [
              tr [] [
                  hcol "pr-4" "#"
                  hcol "pr-4" "Team"
                  hcol "pr-4 text-center" "G"
                  hcol "pr-4 text-center" "W"
                  hcol "pr-4 text-center" "L"
                  hcol "pr-4 text-center" "PCT"
                  hcol "pr-4 text-center" "GB"
                  hcol "text-center" "Streak"
              ]
          ]
          tbody [] [ for s in standings -> row s ]
       ]
      ]

      section [ _class "mt-12" ] [
          h2 [ _class "mb-4 border-l-4 border-barracuda-accent pl-3 text-xl font-black uppercase tracking-tight text-ink-strong" ] [ str "Team Stats" ]
          div [ _class "grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4" ] [
              for s in teamStats -> statCard s
          ]
      ] ]
