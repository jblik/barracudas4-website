module Barracudas.Web.Views.Pages.Schedule

open Giraffe.ViewEngine
open Barracudas.Web.Domain
open Barracudas.Web.Views.Components

let private tableHead =
    thead [ _class "border-b border-barracuda-accent/40 text-xs font-bold uppercase tracking-wider text-accent-text" ] [
        tr [] [
            th [ _class "pb-2 pr-4" ] [ str "Date" ]
            th [ _class "pb-2 pr-4" ] [ str "Matchup" ]
            th [ _class "pb-2 pr-4" ] [ str "Location" ]
            th [ _class "pb-2 pr-4" ] [ str "Score" ]
            th [ _class "pb-2" ] [ str "Status" ]
        ]
    ]

/// The swappable table body (also returned by /schedule/partial).
let table (games: Game list) : XmlNode =
    if List.isEmpty games then
        p [ _class "py-6 text-ink-muted" ] [ str "No games to show." ]
    else
        div [ _class "overflow-x-auto" ] [
            table [ _class "w-full min-w-[34rem] text-left text-sm" ] [
                tableHead
                tbody [] [ for g in games -> gameRow g ]
            ]
        ]

/// Selected tab is gold, the others green.
let private tab (label: string) (key: string) (active: string) =
    let activeCls = if key = active then "bg-barracuda-accent text-barracuda-dark shadow" else "bg-barracuda-light text-white hover:bg-barracuda-line"
    button [ _class (sprintf "rounded-md px-3 py-1.5 text-sm font-bold uppercase tracking-wide transition-colors %s" activeCls)
             _hxGet (sprintf "/schedule/partial?tab=%s" key)
             _hxTarget "#schedule-content"
             _hxSwap "outerHTML" ] [ str label ]

/// Tabs + table together, so an HTMX swap re-renders the tab highlight too
/// (also returned by /schedule/partial).
let content (activeTab: string) (games: Game list) : XmlNode =
    div [ _id "schedule-content" ] [
        div [ _class "mb-6 flex gap-2" ] [
            tab "Upcoming" "upcoming" activeTab
            tab "Results" "past" activeTab
            tab "All" "all" activeTab
        ]
        table games
    ]

let view (activeTab: string) (games: Game list) : XmlNode list =
    [ pageHeader "Schedule" (sprintf "%i season" System.DateTime.Now.Year)
      content activeTab games ]
