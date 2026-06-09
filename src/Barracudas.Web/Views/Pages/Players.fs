module Barracudas.Web.Views.Pages.Players

open Giraffe.ViewEngine
open Barracudas.Web.Domain
open Barracudas.Web.Views.Components

let private num (n: int option) = match n with Some n -> sprintf "#%d" n | None -> "—"

let private row (p: PlayerStat) =
    tr [ _class "border-b border-barracuda-line/40 transition-colors hover:bg-barracuda-light/40" ] [
        td [ _class "py-3 pr-4 font-bold text-barracuda-accent" ] [ str (num p.Number) ]
        td [ _class "py-3 pr-4 font-semibold text-white" ] [
            a [ _href (sprintf "/players/%s" p.Id); _class "hover:text-barracuda-accent" ] [ str p.Name ]
        ]
        td [ _class "py-3 pr-4 text-emerald-200/70" ] [ str p.Position ]
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
          thead [ _class "border-b border-barracuda-accent/40 text-xs font-bold uppercase tracking-wider text-barracuda-accent" ] [
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
    div [ _class "rounded-lg bg-barracuda-light/60 p-4 text-center ring-1 ring-barracuda-accent/30 transition-colors hover:ring-barracuda-accent/70" ] [
        div [ _class "text-3xl font-black text-barracuda-accent" ] [ str value ]
        div [ _class "mt-1 text-xs font-semibold uppercase tracking-wide text-emerald-200/70" ] [ str label ]
    ]

let detailView (p: PlayerStat) : XmlNode list =
    [ a [ _href "/players"; _class "text-sm font-semibold text-barracuda-accent hover:text-barracuda-gold" ] [ str "← All players" ]
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
