module Barracudas.Web.Content

open System.IO
open System.Text.Json
open System.Text.Json.Serialization

// Editable "About" content, loaded from content/about.json (not from EasyScore).

type Practice =
    { Day: string
      Time: string
      Location: string
      Lat: float option
      Lon: float option }
type Contact = { Role: string; Name: string; Email: string }

type AboutContent =
    { Intro: string
      Practices: Practice list
      Contacts: Contact list }

let private options =
    let o = JsonSerializerOptions(PropertyNameCaseInsensitive = true)
    o.Converters.Add(JsonFSharpConverter())
    o

let private fallback =
    { Intro = "About content not found."
      Practices = []
      Contacts = [] }

/// Load About content from the given path, falling back gracefully if missing.
let loadAbout (path: string) : AboutContent =
    try
        if File.Exists path then
            JsonSerializer.Deserialize<AboutContent>(File.ReadAllText path, options)
        else fallback
    with _ -> fallback
