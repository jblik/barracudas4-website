module Barracudas.Web.Views.Pages.Players

open Giraffe.ViewEngine
open Barracudas.Web.Domain
open Barracudas.Web.Views.Components

let private num (n: int option) =
    match n with
    | Some n -> sprintf "#%d" n
    | None -> "—"

let private orDash (s: string) = if s = "" then "—" else s

/// Batting side / throwing arm, e.g. "R/R".
let private batsThrows (p: Player) =
    match p.Bats, p.Throws with
    | "", "" -> "—"
    | b, t -> sprintf "%s/%s" (orDash b) (orDash t)

let private row (p: Player) =
    tr
        [ _class "border-b border-line transition-colors hover:bg-row-hover" ]
        [ td [ _class "py-3 pr-4 font-bold text-accent-text" ] [ str (num p.Number) ]
          td
              [ _class "py-3 pr-4 font-semibold text-ink-strong" ]
              [ a [ _href (sprintf "/players/%s" p.Id); _class "hover:text-accent-text" ] [ str p.ListName ] ]
          td [ _class "py-3 pr-4 text-center" ] [ str (batsThrows p) ]
          td [ _class "py-3 text-center font-bold text-accent-text" ] [ str (defaultArg p.BattingAvg "—") ] ]

let listView (players: Player list) : XmlNode list =
    [ pageHeader "Players" "Team roster"
      if List.isEmpty players then
          p
              [ _class "rounded-lg bg-card p-6 text-ink-muted ring-1 ring-card-ring" ]
              [ str "The roster couldn't be loaded right now. Please check back later." ]
      else
          div
              [ _class "overflow-x-auto" ]
              [ table
                    [ _class "w-full min-w-[24rem] text-left text-sm" ]
                    [ thead
                          [ _class
                                "border-b border-barracuda-accent/40 text-xs font-bold uppercase tracking-wider text-accent-text" ]
                          [ tr
                                []
                                [ th [ _class "pb-2 pr-4" ] [ str "No." ]
                                  th [ _class "pb-2 pr-4" ] [ str "Name" ]
                                  th [ _class "pb-2 pr-4 text-center" ] [ str "B/T" ]
                                  th [ _class "pb-2 text-center" ] [ str "AVG" ] ] ]
                      tbody [] [ for p in players -> row p ] ] ] ]

let private statBox (label: string) (value: string) =
    div
        [ _class
              "rounded-lg bg-card p-4 text-center ring-1 ring-card-ring transition-colors hover:ring-barracuda-accent/70" ]
        [ div [ _class "text-3xl font-black text-accent-text" ] [ str value ]
          div [ _class "mt-1 text-xs font-semibold uppercase tracking-wide text-ink-muted" ] [ str label ] ]

/// One titled stat table; columns and the single season row come as pairs.
let private statTable (title: string) (cells: (string * string) list) =
    section
        [ _class "mt-8" ]
        [ h2 [ _class "mb-3 text-lg font-black uppercase tracking-tight text-ink-strong" ] [ str title ]
          div
              [ _class "overflow-x-auto rounded-lg bg-card ring-1 ring-card-ring" ]
              [ table
                    [ _class "w-full text-center text-sm" ]
                    [ thead
                          [ _class
                                "border-b border-barracuda-accent/40 text-xs font-bold uppercase tracking-wider text-accent-text" ]
                          [ tr [] [ for label, _ in cells -> th [ _class "px-3 py-2" ] [ str label ] ] ]
                      tbody
                          []
                          [ tr
                                []
                                [ for _, value in cells ->
                                      td
                                          [ _class "whitespace-nowrap px-3 py-3 font-semibold text-ink-strong" ]
                                          [ str value ] ] ] ] ] ]

let private battingTable (s: BattingStats) =
    statTable
        "Batting"
        [ "G", string s.Games
          "PA", s.PA
          "AB", s.AB
          "R", s.R
          "H", s.H
          "2B", s.Doubles
          "3B", s.Triples
          "HR", s.HR
          "RBI", s.RBI
          "TB", s.TB
          "BB", s.BB
          "SO", s.SO
          "HBP", s.HBP
          "SB", s.SB
          "CS", s.CS
          "AVG", s.AVG
          "OBP", s.OBP
          "SLG", s.SLG
          "OPS", s.OPS ]

let private fieldingTable (s: FieldingStats) =
    statTable
        "Fielding"
        [ "G", string s.Games
          "IP", s.Innings
          "PO", s.Putouts
          "A", s.Assists
          "OA", s.OutfieldAssists
          "E", s.Errors
          "DP", s.DoublePlays
          "PB", s.PassedBalls
          "SB Att", s.StealAttempts
          "CS", s.CaughtStealing
          "RF", s.RangeFactor
          "FPCT", s.FieldingPct ]

let private pitchingTable (s: PitchingStats) =
    statTable
        "Pitching"
        [ "G", s.Games
          "GS", s.Starts
          "IP", s.IP
          "H", s.H
          "R", s.R
          "ER", s.ER
          "BB", s.BB
          "SO", s.SO
          "HBP", s.HBP
          "WP", s.WildPitches
          "W–L", s.Record
          "SV", s.Saves
          "BF", s.BattersFaced
          "OPP AVG", s.OppAVG
          "WHIP", s.WHIP
          "ERA", s.ERA ]

let detailView (pl: Player) (stats: PlayerStats) : XmlNode list =
    [ a
          [ _href "/players"
            _class "text-sm font-semibold text-accent-text hover:underline" ]
          [ str "← All players" ]
      pageHeader pl.Name (num pl.Number)
      div
          [ _class "grid grid-cols-2 gap-4 sm:grid-cols-3" ]
          [ statBox "Bats" (orDash pl.Bats)
            statBox "Throws" (orDash pl.Throws)
            statBox "AVG" (defaultArg pl.BattingAvg "—") ]
          

      match stats.Batting with
      | Some b -> battingTable b
      | None -> ()
      match stats.Fielding with
      | Some f -> fieldingTable f
      | None -> ()
      match stats.Pitching with
      | Some p -> pitchingTable p
      | None -> ()
      
      
      
      match stats.Batting, stats.Fielding, stats.Pitching with
      | Some b, _, _ -> failwith "todo"
      | _, Some f, Some value1 -> failwith "todo"
      | Some value, Some value1, None -> failwith "todo"
      | Some value, Some value1, Some value2 -> failwith "todo"
      | None, None, None ->
          p [ _class "mt-8 rounded-lg bg-card p-6 text-ink-muted ring-1 ring-card-ring" ] [ str "No season stats yet." ] ]

      // if stats.Batting.IsNone && stats.Fielding.IsNone && stats.Pitching.IsNone then
          // p [ _class "mt-8 rounded-lg bg-card p-6 text-ink-muted ring-1 ring-card-ring" ] [ str "No season stats yet." ] ]
