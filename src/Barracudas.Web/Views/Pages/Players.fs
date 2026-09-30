module Barracudas.Web.Views.Pages.Players

open Giraffe.ViewEngine
open Barracudas.Web.Domain
open Barracudas.Web.Views.Components

let private num (n: int option) = match n with Some n -> sprintf "#%d" n | None -> "—"
let private orDash (s: string) = if s = "" then "—" else s
let private ageText (a: int option) = match a with Some a -> string a | None -> "—"

/// Batting side / throwing arm, e.g. "R/R".
let private batsThrows (p: Player) =
    match p.Bats, p.Throws with
    | "", "" -> "—"
    | b, t -> sprintf "%s/%s" (orDash b) (orDash t)

let private row (p: Player) =
    tr [ _class "border-b border-line transition-colors hover:bg-row-hover" ] [
        td [ _class "py-3 pr-4 font-bold text-accent-text" ] [ str (num p.Number) ]
        td [ _class "py-3 pr-4 font-semibold text-ink-strong" ] [
            a [ _href (sprintf "/players/%s" p.Id); _class "hover:text-accent-text" ] [ str p.Name ]
        ]
        td [ _class "py-3 pr-4 text-center" ] [ str (batsThrows p) ]
        td [ _class "py-3 pr-4 text-center" ] [ str (orDash p.Nationality) ]
        td [ _class "py-3 text-center" ] [ str (ageText p.Age) ]
    ]

let listView (players: Player list) : XmlNode list =
    [ pageHeader "Players" "Team roster"
      if List.isEmpty players then
        p [ _class "rounded-lg bg-card p-6 text-ink-muted ring-1 ring-card-ring" ] [
            str "The roster couldn't be loaded right now. Please check back later."
        ]
      else
       div [ _class "overflow-x-auto" ] [
        table [ _class "w-full min-w-[28rem] text-left text-sm" ] [
          thead [ _class "border-b border-barracuda-accent/40 text-xs font-bold uppercase tracking-wider text-accent-text" ] [
              tr [] [
                  th [ _class "pb-2 pr-4" ] [ str "No." ]
                  th [ _class "pb-2 pr-4" ] [ str "Name" ]
                  th [ _class "pb-2 pr-4 text-center" ] [ str "B/T" ]
                  th [ _class "pb-2 pr-4 text-center" ] [ str "Nat." ]
                  th [ _class "pb-2 text-center" ] [ str "Age" ]
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

let detailView (p: Player) : XmlNode list =
    [ a [ _href "/players"; _class "text-sm font-semibold text-accent-text hover:underline" ] [ str "← All players" ]
      pageHeader p.Name (num p.Number)
      div [ _class "grid grid-cols-2 gap-4 sm:grid-cols-4" ] [
          statBox "Bats" (orDash p.Bats)
          statBox "Throws" (orDash p.Throws)
          statBox "Nationality" (orDash p.Nationality)
          statBox "Age" (ageText p.Age)
      ] ]
