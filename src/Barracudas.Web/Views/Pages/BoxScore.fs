module Barracudas.Web.Views.Pages.BoxScore

open Giraffe.ViewEngine
open Barracudas.Web.Domain
open Barracudas.Web.Views.Components

let private logo (url: string option) (alt: string) (size: string) =
    match url with
    | Some u ->
        img
            [ _src u
              _alt alt
              _class (sprintf "%s shrink-0 rounded-full bg-white/80 object-contain p-0.5 ring-1 ring-line" size) ]
    | None -> span [] []

let private orDash (s: string) = if s = "" then "—" else s

/// Batting averages arrive as ".294" / ".000", or a digit-less placeholder
/// (".---", "-.-") when there are no at-bats yet — show those as a dash.
let private avgText (s: string) =
    if s |> Seq.exists System.Char.IsDigit then s else "—"

/// Final runs for one side: from the linescore if present, else the totals row.
let private runsOf (bs: BoxScore) (team: BoxTeam) (isAway: bool) =
    match bs.LineScore with
    | Some ls -> if isAway then ls.Away.Runs else ls.Home.Runs
    | None ->
        team.Batters
        |> List.tryFind (fun b -> b.Order.IsNone)
        |> Option.map _.R
        |> Option.defaultValue 0

// --- Scoreboard header --------------------------------------------------------

let private scoreboard (bs: BoxScore) =
    let awayRuns = runsOf bs bs.Away true
    let homeRuns = runsOf bs bs.Home false
    let maybeInningsPlayed = bs.LineScore |> Option.map _.Innings

    let side (t: BoxTeam) (runs: int) (won: bool) (alignRight: bool) =
        let nameCls =
            if won then
                "font-black text-ink-strong"
            else
                "font-semibold text-ink"

        // Centred on mobile; pinned to the outer edge (logo+name then score) on
        // desktop, mirrored for the home side so both runs hug the centre word.
        div
            [ _class (
                  sprintf
                      "flex flex-1 items-center justify-center gap-3 sm:justify-start %s"
                      (if alignRight then "sm:flex-row-reverse sm:text-right" else "sm:flex-row")
              ) ]
            [ logo t.Logo t.Name "h-10 w-10"
              span [ _class $"text-lg %s{nameCls}" ] [ str t.Name ]
              span
                  [ _class (
                        sprintf
                            "text-4xl font-black tabular-nums %s"
                            (if won then "text-accent-text" else "text-ink-muted")
                    ) ]
                  [ str (string runs) ] ]

    section
        [ _class "rounded-lg bg-card p-5 ring-1 ring-card-ring" ]
        [ div
              [ _class "flex flex-col items-stretch gap-3 sm:flex-row sm:items-center sm:gap-4" ]
              [ side bs.Away awayRuns (awayRuns > homeRuns) false
                span
                    [ _class "shrink-0 text-center text-sm font-bold uppercase tracking-wide text-ink-muted" ]
                    [ str (
                          "Final"
                          + (maybeInningsPlayed |> Option.map (fun ip -> $" / {ip}") |> Option.defaultValue "")
                      ) ]
                side bs.Home homeRuns (homeRuns > awayRuns) true ] ]

// --- Linescore grid -----------------------------------------------------------

let private lineScoreTable (ls: LineScore) =
    let th' extra t =
        th [ _class (sprintf "whitespace-nowrap px-2.5 py-2 %s" extra) ] [ str t ]

    let total cls v =
        td [ _class (sprintf "px-2.5 py-2 tabular-nums %s" cls) ] [ str (string v) ]

    let row (t: LineScoreTeam) =
        tr
            [ _class "border-b border-line last:border-0" ]
            ([ td
                   [ _class "px-2.5 py-2 text-left font-bold text-ink-strong whitespace-nowrap" ]
                   [ span [ _class "inline-flex items-center gap-1.5" ] [ logo t.Logo t.Name "h-5 w-5"; str t.Abbr ] ] ]
             @ [ for v in t.Innings -> td [ _class "px-2.5 py-2 tabular-nums text-ink" ] [ str (orDash v) ] ]
             @ [ total "font-black text-accent-text" t.Runs
                 total "text-ink" t.Hits
                 total "text-ink" t.Errors ])

    div
        [ _class "overflow-x-auto rounded-lg bg-card ring-1 ring-card-ring" ]
        [ table
              [ _class "w-full text-center text-sm" ]
              [ thead
                    [ _class "border-b border-barracuda-accent/40 text-xs font-bold tracking-wider text-accent-text" ]
                    [ tr
                          []
                          ([ th' "text-left" "" ]
                           @ [ for i in 1 .. ls.Innings -> th' "" (string i) ]
                           @ [ th' "" "R"; th' "" "H"; th' "" "E" ]) ]
                tbody [] [ row ls.Away; row ls.Home ] ] ]

// --- Batting / pitching tables ------------------------------------------------

let private headRow (cols: (string * string * string) list) =
    thead
        [ _class "border-b border-barracuda-accent/40 text-xs font-bold tracking-wider text-accent-text" ]
        [ tr
              []
              [ for label, full, align in cols ->
                    th
                        [ _class (sprintf "cursor-help whitespace-nowrap px-2.5 py-2 %s" align)
                          _title full ]
                        [ str label ] ] ]

let private battingTable (t: BoxTeam) =
    let cols =
        [ "Batters", "Batters", "text-left"
          "AB", "At Bats", "text-center"
          "R", "Runs", "text-center"
          "H", "Hits", "text-center"
          "RBI", "Runs Batted In", "text-center"
          "BB", "Walks (Base on Balls)", "text-center"
          "SO", "Strikeouts", "text-center"
          "LOB", "Left on Base", "text-center"
          "AVG", "Batting Average (season to date)", "text-center" ]

    let num (n: int) =
        td [ _class "px-2.5 py-2 text-center tabular-nums text-ink" ] [ str (string n) ]

    // Our roster has player pages; opponents don't, so only our names link out.
    let nameCell (b: BoxBatter) =
        match (if t.IsUs then b.PlayerId else None) with
        | Some pid -> a [ _href $"/players/%d{pid}"; _class "hover:text-accent-text hover:underline" ] [ str b.Name ]
        | None -> str b.Name

    let batterRow (b: BoxBatter) =
        match b.Order with
        | None ->
            tr
                [ _class "border-t-2 border-barracuda-accent/40 font-black text-ink-strong" ]
                [ td [ _class "px-2.5 py-2 text-left" ] [ str "Totals" ]
                  td [ _class "px-2.5 py-2 text-center tabular-nums" ] [ str (string b.AB) ]
                  td [ _class "px-2.5 py-2 text-center tabular-nums" ] [ str (string b.R) ]
                  td [ _class "px-2.5 py-2 text-center tabular-nums" ] [ str (string b.H) ]
                  td [ _class "px-2.5 py-2 text-center tabular-nums" ] [ str (string b.RBI) ]
                  td [ _class "px-2.5 py-2 text-center tabular-nums" ] [ str (string b.BB) ]
                  td [ _class "px-2.5 py-2 text-center tabular-nums" ] [ str (string b.SO) ]
                  td [ _class "px-2.5 py-2 text-center tabular-nums" ] [ str (string b.LOB) ]
                  td [ _class "px-2.5 py-2 text-center" ] [ str "" ] ]
        | Some spot ->
            tr
                [ _class "border-b border-line transition-colors hover:bg-row-hover" ]
                [ td
                      [ _class "px-2.5 py-2 text-left whitespace-nowrap font-semibold text-ink-strong" ]
                      [ span
                            [ _class "inline-flex items-baseline gap-1.5" ]
                            [ span
                                  [ _class "w-4 text-right text-xs tabular-nums text-ink-muted" ]
                                  [ str (if b.IsSub then "" else string spot) ]
                              span [ _class (if b.IsSub then "pl-3" else "") ] [ nameCell b ]
                              span [ _class "text-xs font-medium text-ink-muted" ] [ str b.Pos ] ] ]
                  num b.AB
                  num b.R
                  num b.H
                  num b.RBI
                  num b.BB
                  num b.SO
                  num b.LOB
                  td [ _class "px-2.5 py-2 text-center tabular-nums text-ink-muted" ] [ str (avgText b.Avg) ] ]

    div
        [ _class "overflow-x-auto rounded-lg bg-card ring-1 ring-card-ring" ]
        [ table [ _class "w-full text-sm" ] [ headRow cols; tbody [] [ for b in t.Batters -> batterRow b ] ] ]

let private pitchingTable (t: BoxTeam) =
    let cols =
        [ "Pitchers", "Pitchers", "text-left"
          "IP", "Innings Pitched", "text-center"
          "H", "Hits Allowed", "text-center"
          "R", "Runs Allowed", "text-center"
          "ER", "Earned Runs", "text-center"
          "BB", "Walks (Base on Balls)", "text-center"
          "SO", "Strikeouts", "text-center"
          "HR", "Home Runs Allowed", "text-center"
          "BF", "Batters Faced", "text-center"
          "NP", "Number of Pitches (Strikes)", "text-center"
          "ERA", "Earned Run Average (season to date)", "text-center" ]

    let pitcherRow (p: BoxPitcher) =
        let rowCls =
            if p.IsTotals then
                "border-t-2 border-barracuda-accent/40 font-black text-ink-strong"
            else
                "border-b border-line transition-colors last:border-0 hover:bg-row-hover"

        let nameCls =
            if p.IsTotals then
                "px-2.5 py-2 text-left whitespace-nowrap"
            else
                "px-2.5 py-2 text-left whitespace-nowrap font-semibold text-ink-strong"

        let num (s: string) =
            td
                [ _class (sprintf "px-2.5 py-2 text-center tabular-nums %s" (if p.IsTotals then "" else "text-ink")) ]
                [ str s ]

        tr
            [ _class rowCls ]
            [ td [ _class nameCls ] [ str p.Name ]
              num p.IP
              num (string p.H)
              num (string p.R)
              num (string p.ER)
              num (string p.BB)
              num (string p.SO)
              num (string p.HR)
              num (string p.BattersFaced)
              num (sprintf "%d (%d)" p.Pitches p.Strikes)
              td
                  [ _class "px-2.5 py-2 text-center tabular-nums text-ink-muted" ]
                  [ str (if p.IsTotals then "" else p.ERA) ] ]

    div
        [ _class "overflow-x-auto rounded-lg bg-card ring-1 ring-card-ring" ]
        [ table [ _class "w-full text-sm" ] [ headRow cols; tbody [] [ for p in t.Pitchers -> pitcherRow p ] ] ]

let private teamSection (t: BoxTeam) (isUs: bool) =
    section
        [ _class "mt-8" ]
        [ h2
              [ _class "mb-3 flex items-center gap-2 text-lg font-black uppercase tracking-tight text-ink-strong" ]
              [ logo t.Logo t.Name "h-7 w-7"
                span [ _class (if isUs then "text-accent-text" else "") ] [ str t.Name ] ]
          battingTable t
          div [ _class "mt-4" ] [ pitchingTable t ] ]

// --- Game notes ---------------------------------------------------------------

/// Full category name + tooltip explanation for each note abbreviation.
let private noteMeta =
    dict
        [ "2B", ("Doubles", "Doubles (2B) — two-base hits")
          "3B", ("Triples", "Triples (3B) — three-base hits")
          "HR", ("Home Runs", "Home runs (HR)")
          "SF", ("Sacrifice Flies", "Sacrifice flies (SF)")
          "GIDP", ("Grounded Into Double Play", "Grounded into double play (GIDP)")
          "SB", ("Stolen Bases", "Stolen bases (SB)")
          "CS", ("Caught Stealing", "Caught stealing (CS)")
          "E", ("Errors", "Fielding errors (E)")
          "DP", ("Double Plays", "Double plays turned (DP)")
          "PB", ("Passed Balls", "Passed balls (PB)")
          "WP", ("Wild Pitches", "Wild pitches (WP)")
          "HBP", ("Hit By Pitch", "Hit by pitch (HBP)")
          "BK", ("Balks", "Balks (BK)") ]

let private notesSection (notes: BoxNote list) (away: BoxTeam) (home: BoxTeam) =
    // The opponent's name is tinted with their brand colour; ours stays gold.
    let teamLine (t: BoxTeam) (text: string option) =
        match text with
        | Some s ->
            let nameAttrs =
                match t.Color with
                | Some c -> [ _class "font-bold"; _style $"color:%s{c}" ]
                | None -> [ _class "font-bold text-accent-text" ]

            [ p
                  [ _class "leading-snug" ]
                  [ span nameAttrs [ str (t.Abbr + ":") ]
                    span [ _class "ml-1.5 font-semibold text-ink-strong" ] [ str s ] ] ]
        | None -> []

    section
        [ _class "mt-8" ]
        [ h2 [ _class "mb-3 text-lg font-black uppercase tracking-tight text-ink-strong" ] [ str "Game Notes" ]
          div
              [ _class "space-y-3" ]
              [ for n in notes ->
                    let full, tip =
                        match noteMeta.TryGetValue n.Label with
                        | true, v -> v
                        | _ -> n.Label, n.Label

                    div
                        [ _class "rounded-lg bg-card p-4 text-sm text-ink ring-1 ring-card-ring" ]
                        ([ h3
                               [ _class "mb-1.5 inline-block cursor-help text-sm font-black uppercase tracking-wide text-ink-strong"
                                 _title tip ]
                               [ str full ] ]
                         @ teamLine away n.Away
                         @ teamLine home n.Home) ] ]

// --- Page ---------------------------------------------------------------------

let view (bs: BoxScore) : XmlNode list =
    let subtitle =
        [ bs.Date.ToString "dddd, MMMM d, yyyy"; bs.Location; bs.Round ]
        |> List.filter (fun s -> s <> "")
        |> String.concat " · "

    [ a
          [ _href "/schedule"
            _class "text-sm font-semibold text-accent-text hover:underline" ]
          [ str "← Schedule" ]
      pageHeader "Box Score" subtitle
      scoreboard bs
      match bs.LineScore with
      | Some ls -> div [ _class "mt-6" ] [ lineScoreTable ls ]
      | None -> ()
      teamSection bs.Away bs.Away.IsUs
      teamSection bs.Home bs.Home.IsUs
      if not (List.isEmpty bs.Notes) then
          notesSection bs.Notes bs.Away bs.Home
      if not (String.length bs.Umpires = 0 && String.length bs.Scorer = 0) then
          p
              [ _class "mt-8 text-xs text-ink-muted" ]
              [ if bs.Umpires <> "" then
                    str (sprintf "Umpires: %s" bs.Umpires)
                if bs.Umpires <> "" && bs.Scorer <> "" then
                    str "  ·  "
                if bs.Scorer <> "" then
                    str (sprintf "Scorer: %s" bs.Scorer) ] ]
