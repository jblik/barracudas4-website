module Barracudas.Web.Views.Pages.Standings

open Giraffe.ViewEngine
open Barracudas.Web.Domain
open Barracudas.Web.Views.Components

let private row (s: Standing) =
    let rowCls =
        if s.IsUs then "border-b border-line border-l-4 border-l-barracuda-accent bg-barracuda-accent/15 font-bold"
        else "border-b border-line transition-colors hover:bg-row-hover"
    let teamCls = if s.IsUs then "py-3 pr-4 text-accent-text" else "py-3 pr-4 text-ink-strong"
    tr [ _class rowCls ] [
        td [ _class "py-3 pr-4 text-ink-muted" ] [ str (string s.Rank) ]
        td [ _class teamCls ] [ str s.Team ]
        td [ _class "py-3 pr-4 text-center" ] [ str (string s.Wins) ]
        td [ _class "py-3 pr-4 text-center" ] [ str (string s.Losses) ]
        td [ _class "py-3 pr-4 text-center" ] [ str (s.Pct.ToString "0.000") ]
        td [ _class "py-3 text-center text-ink-muted" ] [ str (if s.GamesBehind = 0.0 then "—" else s.GamesBehind.ToString "0.0") ]
    ]

let view (standings: Standing list) (teamStats: TeamStat list) : XmlNode list =
    [ pageHeader "Standings & Stats" "1. Liga"
      div [ _class "overflow-x-auto" ] [
       table [ _class "w-full min-w-[34rem] text-left text-sm" ] [
          thead [ _class "border-b border-barracuda-accent/40 text-xs font-bold uppercase tracking-wider text-accent-text" ] [
              tr [] [
                  th [ _class "pb-2 pr-4" ] [ str "#" ]
                  th [ _class "pb-2 pr-4" ] [ str "Team" ]
                  th [ _class "pb-2 pr-4 text-center" ] [ str "W" ]
                  th [ _class "pb-2 pr-4 text-center" ] [ str "L" ]
                  th [ _class "pb-2 pr-4 text-center" ] [ str "PCT" ]
                  th [ _class "pb-2 text-center" ] [ str "GB" ]
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
