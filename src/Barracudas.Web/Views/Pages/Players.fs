module Barracudas.Web.Views.Pages.Players

open Giraffe.ViewEngine
open Barracudas.Web.Domain
open Barracudas.Web.Views.Components

let private num (n: int option) = match n with Some n -> sprintf "#%d" n | None -> "—"

let private row (p: PlayerStat) =
    tr [ _class "border-b border-slate-700 hover:bg-slate-800/50" ] [
        td [ _class "py-3 pr-4 text-slate-400" ] [ str (num p.Number) ]
        td [ _class "py-3 pr-4 font-medium text-white" ] [
            a [ _href (sprintf "/players/%s" p.Id); _class "hover:text-barracuda-accent" ] [ str p.Name ]
        ]
        td [ _class "py-3 pr-4 text-slate-400" ] [ str p.Position ]
        td [ _class "py-3 pr-4 text-center" ] [ str (string p.Games) ]
        td [ _class "py-3 pr-4 text-center" ] [ str (string p.AtBats) ]
        td [ _class "py-3 pr-4 text-center" ] [ str (string p.Hits) ]
        td [ _class "py-3 pr-4 text-center" ] [ str (string p.HomeRuns) ]
        td [ _class "py-3 pr-4 text-center" ] [ str (string p.Rbi) ]
        td [ _class "py-3 text-center font-semibold text-white" ] [ str (p.Avg.ToString "0.000") ]
    ]

let listView (players: PlayerStat list) : XmlNode list =
    [ pageHeader "Players" "Individual batting stats"
      table [ _class "w-full text-left text-sm" ] [
          thead [ _class "text-xs uppercase tracking-wide text-slate-500" ] [
              tr [] [
                  th [ _class "pb-2 pr-4" ] [ str "No." ]
                  th [ _class "pb-2 pr-4" ] [ str "Name" ]
                  th [ _class "pb-2 pr-4" ] [ str "Pos" ]
                  th [ _class "pb-2 pr-4 text-center" ] [ str "G" ]
                  th [ _class "pb-2 pr-4 text-center" ] [ str "AB" ]
                  th [ _class "pb-2 pr-4 text-center" ] [ str "H" ]
                  th [ _class "pb-2 pr-4 text-center" ] [ str "HR" ]
                  th [ _class "pb-2 pr-4 text-center" ] [ str "RBI" ]
                  th [ _class "pb-2 text-center" ] [ str "AVG" ]
              ]
          ]
          tbody [] [ for p in players -> row p ]
      ] ]

let private statBox (label: string) (value: string) =
    div [ _class "rounded-lg bg-slate-800 p-4 text-center ring-1 ring-slate-700" ] [
        div [ _class "text-2xl font-bold text-barracuda-accent" ] [ str value ]
        div [ _class "mt-1 text-xs uppercase tracking-wide text-slate-400" ] [ str label ]
    ]

let detailView (p: PlayerStat) : XmlNode list =
    [ a [ _href "/players"; _class "text-sm text-slate-400 hover:text-white" ] [ str "← All players" ]
      pageHeader p.Name (sprintf "%s · %s" (num p.Number) p.Position)
      div [ _class "grid grid-cols-2 gap-4 sm:grid-cols-4" ] [
          statBox "Games" (string p.Games)
          statBox "At Bats" (string p.AtBats)
          statBox "Runs" (string p.Runs)
          statBox "Hits" (string p.Hits)
          statBox "Home Runs" (string p.HomeRuns)
          statBox "RBI" (string p.Rbi)
          statBox "AVG" (p.Avg.ToString "0.000")
      ] ]
