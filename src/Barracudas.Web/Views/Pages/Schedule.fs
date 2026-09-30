module Barracudas.Web.Views.Pages.Schedule

open Giraffe.ViewEngine
open Barracudas.Web.Domain
open Barracudas.Web.Views.Components

let private tableHead =
    thead [ _class "text-xs uppercase tracking-wide text-slate-500" ] [
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
        p [ _class "py-6 text-slate-500" ] [ str "No games to show." ]
    else
        table [ _class "w-full text-left text-sm" ] [
            tableHead
            tbody [] [ for g in games -> gameRow g ]
        ]

let private tab (label: string) (key: string) (active: string) =
    let activeCls = if key = active then "bg-barracuda text-white" else "bg-slate-800 text-slate-300 hover:bg-slate-700"
    button [ _class (sprintf "rounded-md px-3 py-1.5 text-sm font-medium %s" activeCls)
             _hxGet (sprintf "/schedule/partial?tab=%s" key)
             _hxTarget "#schedule-body"
             _hxSwap "innerHTML" ] [ str label ]

let view (activeTab: string) (games: Game list) : XmlNode list =
    [ pageHeader "Schedule" (sprintf "%i season" System.DateTime.Now.Year)
      div [ _class "mb-6 flex gap-2" ] [
          tab "Upcoming" "upcoming" activeTab
          tab "Results" "past" activeTab
          tab "All" "all" activeTab
      ]
      div [ _id "schedule-body" ] [ table games ] ]
