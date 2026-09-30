module Barracudas.Web.Views.Pages.About

open Giraffe.ViewEngine
open Barracudas.Web.Content
open Barracudas.Web.Views.Components

/// Location cell. When coordinates are present, hovering reveals an
/// OpenStreetMap popover with a pin and a link to open the full map.
let private locationCell (p: Practice) =
    match p.Lat, p.Lon with
    | Some lat, Some lon ->
        let inv = System.Globalization.CultureInfo.InvariantCulture
        let f (v: float) = v.ToString("0.######", inv)
        // Small bounding box around the marker for a street-level embed.
        let embed =
            sprintf
                "https://www.openstreetmap.org/export/embed.html?bbox=%s,%s,%s,%s&layer=mapnik&marker=%s,%s"
                (f (lon - 0.004)) (f (lat - 0.0025)) (f (lon + 0.004)) (f (lat + 0.0025)) (f lat) (f lon)
        let fullMap = sprintf "https://www.openstreetmap.org/?mlat=%s&mlon=%s#map=17/%s/%s" (f lat) (f lon) (f lat) (f lon)
        td [ _class "py-3 text-emerald-200/70" ] [
            span [ _class "group relative inline-block" ] [
                span [ _class "cursor-help border-b border-dashed border-barracuda-accent/60 transition-colors group-hover:text-barracuda-gold" ] [ str p.Location ]
                // pt-2 (not mt-2) keeps the hover region continuous with the trigger.
                div [ _class "absolute left-0 top-full z-30 hidden pt-2 group-hover:block" ] [
                    div [ _class "w-72 overflow-hidden rounded-lg bg-barracuda ring-1 ring-barracuda-accent/50 shadow-xl shadow-black/50 sm:w-80" ] [
                        iframe [ _src embed
                                 _class "h-44 w-full border-0"
                                 KeyValue("loading", "lazy")
                                 KeyValue("title", sprintf "Map: %s" p.Location) ] []
                        a [ _href fullMap; _target "_blank"; _rel "noopener"
                            _class "block bg-barracuda-dark px-3 py-2 text-xs font-semibold text-barracuda-accent hover:text-barracuda-gold" ] [
                            str "Open in OpenStreetMap ↗"
                        ]
                    ]
                ]
            ]
        ]
    | _ ->
        td [ _class "py-3 text-emerald-200/70" ] [ str p.Location ]

let private practiceRow (p: Practice) =
    tr [ _class "border-b border-barracuda-line/40" ] [
        td [ _class "py-3 pr-4 font-bold text-barracuda-accent" ] [ str p.Day ]
        td [ _class "py-3 pr-4 text-emerald-50" ] [ str p.Time ]
        locationCell p
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
