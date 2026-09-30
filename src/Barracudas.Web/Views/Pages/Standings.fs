module Barracudas.Web.Views.Pages.Standings

open Giraffe.ViewEngine
open Barracudas.Web.Domain
open Barracudas.Web.Views.Components

let private row (s: Standing) =
    let rowCls =
        if s.IsUs then "border-b border-slate-700 bg-barracuda/20 font-semibold"
        else "border-b border-slate-700 hover:bg-slate-800/50"
    tr [ _class rowCls ] [
        td [ _class "py-3 pr-4 text-slate-400" ] [ str (string s.Rank) ]
        td [ _class "py-3 pr-4 text-white" ] [ str s.Team ]
        td [ _class "py-3 pr-4 text-center" ] [ str (string s.Wins) ]
        td [ _class "py-3 pr-4 text-center" ] [ str (string s.Losses) ]
        td [ _class "py-3 pr-4 text-center" ] [ str (s.Pct.ToString "0.000") ]
        td [ _class "py-3 text-center text-slate-400" ] [ str (if s.GamesBehind = 0.0 then "—" else s.GamesBehind.ToString "0.0") ]
    ]

let view (standings: Standing list) (teamStats: TeamStat list) : XmlNode list =
    [ pageHeader "Standings & Stats" "1. Liga"
      table [ _class "w-full text-left text-sm" ] [
          thead [ _class "text-xs uppercase tracking-wide text-slate-500" ] [
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

      section [ _class "mt-12" ] [
          h2 [ _class "mb-4 text-xl font-semibold text-white" ] [ str "Team Stats" ]
          div [ _class "grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4" ] [
              for s in teamStats -> statCard s
          ]
      ] ]
