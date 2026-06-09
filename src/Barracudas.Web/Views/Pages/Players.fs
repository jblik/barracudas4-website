module Barracudas.Web.Views.Pages.Players

open Giraffe.ViewEngine
open Barracudas.Web.Domain
open Barracudas.Web.Views.Components

let private num (n: int option) = match n with Some n -> sprintf "#%d" n | None -> "—"

let private row (p: PlayerStat) =
    tr [ _class "border-b border-line transition-colors hover:bg-row-hover" ] [
        td [ _class "py-3 pr-4 font-bold text-accent-text" ] [ str (num p.Number) ]
        td [ _class "py-3 pr-4 font-semibold text-ink-strong" ] [
            a [ _href (sprintf "/players/%s" p.Id); _class "hover:text-accent-text" ] [ str p.Name ]
        ]
        td [ _class "py-3 pr-4 text-ink-muted" ] [ str p.Position ]
        td [ _class "py-3 pr-4 text-center" ] [ str (string p.Games) ]
        td [ _class "py-3 pr-4 text-center" ] [ str (string p.AtBats) ]
        td [ _class "py-3 pr-4 text-center" ] [ str (string p.Hits) ]
        td [ _class "py-3 pr-4 text-center" ] [ str (string p.HomeRuns) ]
        td [ _class "py-3 pr-4 text-center" ] [ str (string p.Rbi) ]
        td [ _class "py-3 text-center font-semibold text-white" ] [ str (p.Avg.ToString "0.000") ]
    ]

let listView (players: PlayerStat list) : XmlNode list =
    [ pageHeader "Players" "Individual batting stats"
      if List.isEmpty players then
        p [ _class "rounded-lg bg-card p-6 text-ink-muted ring-1 ring-card-ring" ] [
            str "Individual player stats aren't published in the public league feed. They'll appear here once direct EasyScore API access is set up."
        ]
      else
       div [ _class "overflow-x-auto" ] [
        table [ _class "w-full min-w-[40rem] text-left text-sm" ] [
          thead [ _class "border-b border-barracuda-accent/40 text-xs font-bold uppercase tracking-wider text-accent-text" ] [
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
       ]
      ] ]

let private statBox (label: string) (value: string) =
    div [ _class "rounded-lg bg-card p-4 text-center ring-1 ring-card-ring transition-colors hover:ring-barracuda-accent/70" ] [
        div [ _class "text-3xl font-black text-accent-text" ] [ str value ]
        div [ _class "mt-1 text-xs font-semibold uppercase tracking-wide text-ink-muted" ] [ str label ]
    ]

let detailView (p: PlayerStat) : XmlNode list =
    [ a [ _href "/players"; _class "text-sm font-semibold text-accent-text hover:underline" ] [ str "← All players" ]
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
