module Barracudas.Web.Views.Pages.About

open Giraffe.ViewEngine
open Barracudas.Web.Content
open Barracudas.Web.Views.Components

let private practiceRow (p: Practice) =
    tr [ _class "border-b border-slate-700" ] [
        td [ _class "py-3 pr-4 font-medium text-white" ] [ str p.Day ]
        td [ _class "py-3 pr-4 text-slate-300" ] [ str p.Time ]
        td [ _class "py-3 text-slate-400" ] [ str p.Location ]
    ]

let private contactCard (c: Contact) =
    div [ _class "rounded-lg bg-slate-800 p-4 ring-1 ring-slate-700" ] [
        div [ _class "text-sm uppercase tracking-wide text-barracuda-accent" ] [ str c.Role ]
        div [ _class "mt-1 font-semibold text-white" ] [ str c.Name ]
        a [ _href (sprintf "mailto:%s" c.Email); _class "text-sm text-slate-400 hover:text-white" ] [ str c.Email ]
    ]

let view (content: AboutContent) : XmlNode list =
    [ pageHeader "About Us" "Who we are, when we practice, and how to reach us"
      p [ _class "max-w-2xl text-slate-300" ] [ str content.Intro ]

      section [ _class "mt-10" ] [
          h2 [ _class "mb-4 text-xl font-semibold text-white" ] [ str "Practices" ]
          table [ _class "w-full text-left text-sm" ] [
              thead [ _class "text-xs uppercase tracking-wide text-slate-500" ] [
                  tr [] [
                      th [ _class "pb-2 pr-4" ] [ str "Day" ]
                      th [ _class "pb-2 pr-4" ] [ str "Time" ]
                      th [ _class "pb-2" ] [ str "Location" ]
                  ]
              ]
              tbody [] [ for p in content.Practices -> practiceRow p ]
          ]
      ]

      section [ _class "mt-10" ] [
          h2 [ _class "mb-4 text-xl font-semibold text-white" ] [ str "Contacts" ]
          div [ _class "grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3" ] [
              for c in content.Contacts -> contactCard c
          ]
      ] ]
