module Barracudas.Web.Views.Pages.About

open Giraffe.ViewEngine
open Barracudas.Web.Content
open Barracudas.Web.Views.Components

let private practiceRow (p: Practice) =
    tr [ _class "border-b border-barracuda-line/40" ] [
        td [ _class "py-3 pr-4 font-bold text-barracuda-accent" ] [ str p.Day ]
        td [ _class "py-3 pr-4 text-emerald-50" ] [ str p.Time ]
        td [ _class "py-3 text-emerald-200/70" ] [ str p.Location ]
    ]

let private contactCard (c: Contact) =
    div [ _class "rounded-lg bg-barracuda-light/60 p-4 ring-1 ring-barracuda-accent/30 transition-colors hover:ring-barracuda-accent/70" ] [
        div [ _class "text-sm font-bold uppercase tracking-wide text-barracuda-accent" ] [ str c.Role ]
        div [ _class "mt-1 font-semibold text-white" ] [ str c.Name ]
        a [ _href (sprintf "mailto:%s" c.Email); _class "text-sm text-emerald-200/70 hover:text-barracuda-gold" ] [ str c.Email ]
    ]

let view (content: AboutContent) : XmlNode list =
    [ pageHeader "About Us" "Who we are, when we practice, and how to reach us"
      p [ _class "max-w-2xl text-lg leading-relaxed text-emerald-100/90" ] [ str content.Intro ]

      section [ _class "mt-10" ] [
          h2 [ _class "mb-4 border-l-4 border-barracuda-accent pl-3 text-xl font-black uppercase tracking-tight text-white" ] [ str "Practices" ]
          table [ _class "w-full text-left text-sm" ] [
              thead [ _class "border-b border-barracuda-accent/40 text-xs font-bold uppercase tracking-wider text-barracuda-accent" ] [
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
          h2 [ _class "mb-4 border-l-4 border-barracuda-accent pl-3 text-xl font-black uppercase tracking-tight text-white" ] [ str "Contacts" ]
          div [ _class "grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3" ] [
              for c in content.Contacts -> contactCard c
          ]
      ] ]
