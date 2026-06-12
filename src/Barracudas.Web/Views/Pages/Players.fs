module Barracudas.Web.Views.Pages.Players

open Giraffe.ViewEngine
open Barracudas.Web.Domain
open Barracudas.Web.Views.Components

let private num (n: int option) =
    match n with
    | Some n -> $"#%d{n}"
    | None -> "—"

let private orDash (s: string) = if s = "" then "—" else s

/// Batting side / throwing arm, e.g. "R/R".
let private batsThrows (p: Player) =
    match p.Bats, p.Throws with
    | "", "" -> "—"
    | b, t -> sprintf "%s/%s" (orDash b) (orDash t)

/// Comparable cell value for column sorting; Missing rows always go to the bottom.
type private SortKey =
    | Num of float
    | Str of string
    | Missing

/// One roster column: how to render its header and cells, and how to order rows by it.
type private Column =
    { Key: string
      Label: string
      HeaderClass: string
      Cell: Player -> XmlNode
      SortKey: Player -> SortKey }

let private avgKey (p: Player) =
    match p.BattingAvg with
    | Some a ->
        match System.Double.TryParse(a, System.Globalization.CultureInfo.InvariantCulture) with
        | true, v -> Num v
        | false, _ -> Missing
    | None -> Missing

let private columns =
    [ { Key = "no"
        Label = "No."
        HeaderClass = "pb-2 pr-4"
        Cell = fun p -> td [ _class "py-3 pr-4 font-bold text-accent-text" ] [ str (num p.Number) ]
        SortKey =
          fun p ->
              match p.Number with
              | Some n -> Num(float n)
              | None -> Missing }
      { Key = "name"
        Label = "Name"
        HeaderClass = "pb-2 pr-4"
        Cell =
          fun p ->
              td
                  [ _class "py-3 pr-4 font-semibold text-ink-strong" ]
                  [ a [ _href (sprintf "/players/%s" p.Id); _class "hover:text-accent-text" ] [ str p.ListName ] ]
        SortKey = fun p -> Str p.ListName }
      { Key = "bt"
        Label = "B/T"
        HeaderClass = "pb-2 pr-4 text-center"
        Cell = fun p -> td [ _class "py-3 pr-4 text-center" ] [ str (batsThrows p) ]
        SortKey =
          fun p ->
              match batsThrows p with
              | "—" -> Missing
              | s -> Str s }
      { Key = "avg"
        Label = "AVG"
        HeaderClass = "pb-2 text-center"
        Cell =
          fun p -> td [ _class "py-3 text-center font-bold text-accent-text" ] [ str (defaultArg p.BattingAvg "—") ]
        SortKey = avgKey } ]

/// Players already arrive in the default order (last name, first name);
/// no/unknown sort key keeps it.
let private applySort (sort: string option) (desc: bool) (players: Player list) =
    match sort |> Option.bind (fun k -> columns |> List.tryFind (fun c -> c.Key = k)) with
    | None -> players
    | Some col ->
        let missing, present = players |> List.partition (fun p -> col.SortKey p = Missing)

        let sorted =
            if desc then
                present |> List.sortByDescending col.SortKey
            else
                present |> List.sortBy col.SortKey

        sorted @ missing

/// Clicking a header sorts ascending; clicking the active column flips the direction.
let private header (active: string option) (desc: bool) (col: Column) =
    let isActive = active = Some col.Key
    let nextDir = if isActive && not desc then "desc" else "asc"
    let arrow = if isActive then (if desc then " ▼" else " ▲") else ""

    th
        [ _class (col.HeaderClass + " cursor-pointer select-none hover:underline")
          _hxGet (sprintf "/players/partial?sort=%s&dir=%s" col.Key nextDir)
          _hxTarget "#players-table"
          _hxSwap "outerHTML" ]
        [ str (col.Label + arrow) ]

let private row (p: Player) =
    tr [ _class "border-b border-line transition-colors hover:bg-row-hover" ] [ for c in columns -> c.Cell p ]

/// The swappable roster table (also returned by /players/partial).
let rosterTable (sort: string option) (desc: bool) (players: Player list) : XmlNode =
    div
        [ _id "players-table"; _class "overflow-x-auto" ]
        [ table
              [ _class "w-full min-w-[24rem] text-left text-sm" ]
              [ thead
                    [ _class
                          "border-b border-barracuda-accent/40 text-xs font-bold uppercase tracking-wider text-accent-text" ]
                    [ tr [] [ for c in columns -> header sort desc c ] ]
                tbody [] [ for p in applySort sort desc players -> row p ] ] ]

let listView (players: Player list) : XmlNode list =
    [ pageHeader "Players" "Team roster"
      if List.isEmpty players then
          p
              [ _class "rounded-lg bg-card p-6 text-ink-muted ring-1 ring-card-ring" ]
              [ str "The roster couldn't be loaded right now. Please check back later." ]
      else
          rosterTable None false players ]

let private statBox (label: string) (value: string) =
    div
        [ _class
              "rounded-lg bg-card p-4 text-center ring-1 ring-card-ring transition-colors hover:ring-barracuda-accent/70" ]
        [ div [ _class "text-3xl font-black text-accent-text" ] [ str value ]
          div [ _class "mt-1 text-xs font-semibold uppercase tracking-wide text-ink-muted" ] [ str label ] ]

/// One titled stat table; each cell is (abbreviation, full stat name, value) —
/// the full name shows as a hover tooltip on both the header and the value.
let private statTable (title: string) (cells: (string * string * string) list) =
    section
        [ _class "mt-8" ]
        [ h2 [ _class "mb-3 text-lg font-black uppercase tracking-tight text-ink-strong" ] [ str title ]
          div
              [ _class "overflow-x-auto rounded-lg bg-card ring-1 ring-card-ring" ]
              [ table
                    [ _class "w-full text-center text-sm" ]
                    [ thead
                          [ _class
                                "border-b border-barracuda-accent/40 text-xs font-bold tracking-wider text-accent-text" ]
                          [ tr
                                []
                                [ for label, full, _ in cells ->
                                      th [ _class "cursor-help whitespace-nowrap px-3 py-2"; _title full ] [ str label ] ] ]
                      tbody
                          []
                          [ tr
                                []
                                [ for _, full, value in cells ->
                                      td
                                          [ _class "whitespace-nowrap px-3 py-3 font-semibold text-ink-strong"
                                            _title full ]
                                          [ str value ] ] ] ] ] ]

let private battingTable (s: BattingStats) =
    statTable
        "Batting"
        [ "G", "Games", string s.Games
          "PA", "Plate Appearances", s.PA
          "AB", "At Bats", s.AB
          "R", "Runs", s.R
          "H", "Hits", s.H
          "2B", "Doubles", s.Doubles
          "3B", "Triples", s.Triples
          "HR", "Home Runs", s.HR
          "RBI", "Runs Batted In", s.RBI
          "TB", "Total Bases", s.TB
          "BB", "Walks (Base on Balls)", s.BB
          "SO", "Strikeouts", s.SO
          "HBP", "Hit by Pitch", s.HBP
          "SB", "Stolen Bases", s.SB
          "CS", "Caught Stealing", s.CS
          "AVG", "Batting Average", s.AVG
          "OBP", "On-Base Percentage", s.OBP
          "SLG", "Slugging Percentage", s.SLG
          "OPS", "On-Base Plus Slugging", s.OPS ]

let private fieldingTable (s: FieldingStats) =
    statTable
        "Fielding"
        [ "G", "Games", string s.Games
          "IP", "Innings Played", s.Innings
          "PO", "Putouts", s.Putouts
          "A", "Assists", s.Assists
          "OA", "Outfield Assists", s.OutfieldAssists
          "E", "Errors", s.Errors
          "DP", "Double Plays", s.DoublePlays
          "PB", "Passed Balls", s.PassedBalls
          "SB Att", "Stolen Base Attempts", s.StealAttempts
          "CS", "Caught Stealing", s.CaughtStealing
          "RF", "Range Factor", s.RangeFactor
          "FPCT", "Fielding Percentage", s.FieldingPct ]

let private pitchingTable (s: PitchingStats) =
    statTable
        "Pitching"
        [ "G", "Games", s.Games
          "GS", "Games Started", s.Starts
          "IP", "Innings Pitched", s.IP
          "H", "Hits Allowed", s.H
          "R", "Runs Allowed", s.R
          "ER", "Earned Runs", s.ER
          "BB", "Walks (Base on Balls)", s.BB
          "SO", "Strikeouts", s.SO
          "HBP", "Hit Batters", s.HBP
          "WP", "Wild Pitches", s.WildPitches
          "W–L", "Win–Loss Record", s.Record
          "SV", "Saves", s.Saves
          "BF", "Batters Faced", s.BattersFaced
          "OPP AVG", "Opponent Batting Average", s.OppAVG
          "WHIP", "Walks and Hits per Inning Pitched", s.WHIP
          "ERA", "Earned Run Average", s.ERA ]

/// A titled per-game log table in the same style as statTable; each column is
/// (abbreviation, full stat name, cell value) — the full name shows as a hover
/// tooltip on the header and every cell. Columns marked * are season-to-date.
let private logTable (title: string) (columns: (string * string * ('row -> string)) list) (rows: 'row list) =
    section
        [ _class "mt-8" ]
        [ h2 [ _class "mb-3 text-lg font-black uppercase tracking-tight text-ink-strong" ] [ str title ]
          div
              [ _class "overflow-x-auto rounded-lg bg-card ring-1 ring-card-ring" ]
              [ table
                    [ _class "w-full text-center text-sm" ]
                    [ thead
                          [ _class
                                "border-b border-barracuda-accent/40 text-xs font-bold tracking-wider text-accent-text" ]
                          [ tr
                                []
                                [ for label, full, _ in columns ->
                                      th [ _class "cursor-help whitespace-nowrap px-3 py-2"; _title full ] [ str label ] ] ]
                      tbody
                          []
                          [ for row in rows ->
                                tr
                                    [ _class
                                          "border-b border-line transition-colors last:border-0 hover:bg-row-hover" ]
                                    [ for _, full, value in columns ->
                                          td
                                              [ _class "whitespace-nowrap px-3 py-2 font-semibold text-ink-strong"
                                                _title full ]
                                              [ str (value row) ] ] ] ] ] ]

[<Literal>]
let dateformat = "ddd, MMM d"

let private battingLogTable (rows: BattingLogEntry list) =
    logTable
        "Batting Game Log"
        [ "Date", "Game Date", fun (e: BattingLogEntry) -> e.Date.ToString(dateformat)
          "Opponent", "Opponent", _.Opponent
          "Spot", "Batting Order Spot", _.Spot
          "Pos", "Positions Played", _.Pos
          "AB", "At Bats", _.AB
          "R", "Runs", _.R
          "H", "Hits", _.H
          "2B", "Doubles", _.Doubles
          "3B", "Triples", _.Triples
          "HR", "Home Runs", _.HR
          "RBI", "Runs Batted In", _.RBI
          "BB", "Walks (Base on Balls)", _.BB
          "SO", "Strikeouts", _.SO
          "SB", "Stolen Bases", _.SB
          "CS", "Caught Stealing", _.CS
          "HBP", "Hit by Pitch", _.HBP
          "S", "Sacrifice Bunts", _.Sac
          "SF", "Sacrifice Flies", _.SacFlies
          "GIDP", "Grounded into Double Plays", _.GIDP
          "2-out RBI", "Two-Out Runs Batted In", _.TwoOutRBI
          "RISP", "Hits with Runners in Scoring Position", _.RISP
          "GSc", "Game Score", _.GameScore
          "AVG*", "Batting Average (season to date)", _.AvgToDate ]
        rows

let private fieldingLogTable (rows: FieldingLogEntry list) =
    logTable
        "Fielding Game Log"
        [ "Date", "Game Date", fun (e: FieldingLogEntry) -> e.Date.ToString dateformat
          "Opponent", "Opponent", _.Opponent
          "Pos", "Positions Played", _.Pos
          "IP", "Innings Played", _.Innings
          "PO", "Putouts", _.Putouts
          "A", "Assists", _.Assists
          "OA", "Outfield Assists", _.OutfieldAssists
          "E", "Errors", _.Errors
          "DP", "Double Plays", _.DoublePlays
          "PB", "Passed Balls", _.PassedBalls
          "SB Att", "Stolen Base Attempts", _.StealAttempts
          "CS", "Caught Stealing", _.CaughtStealing
          "RF*", "Range Factor (season to date)", _.RangeFactorToDate
          "FPCT*", "Fielding Percentage (season to date)", _.FieldingPctToDate ]
        rows

let private pitchingLogTable (rows: PitchingLogEntry list) =
    logTable
        "Pitching Game Log"
        [ "Date", "Game Date", fun (e: PitchingLogEntry) -> e.Date.ToString dateformat
          "Opponent", "Opponent", _.Opponent
          "IP", "Innings Pitched", _.IP
          "H", "Hits Allowed", _.H
          "R", "Runs Allowed", _.R
          "ER", "Earned Runs", _.ER
          "BB", "Walks (Base on Balls)", _.BB
          "SO", "Strikeouts", _.SO
          "HBP", "Hit Batters", _.HBP
          "WP", "Wild Pitches", _.WildPitches
          "BK", "Balks", _.Balks
          "GB", "Ground Balls", _.GroundBalls
          "FB", "Fly Balls", _.FlyBalls
          "BF", "Batters Faced", _.BattersFaced
          "#Pit", "Pitches Thrown", _.Pitches
          "Dec", "Decision (Win/Loss/Save)", _.Decision
          "Rel", "Relief Decision (Hold/Blown Save)", _.Relief
          "GSc", "Game Score", _.GameScore
          "WHIP*", "Walks and Hits per Inning Pitched (season to date)", _.WhipToDate
          "ERA*", "Earned Run Average (season to date)", _.EraToDate ]
        rows

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
      if not stats.BattingLog.IsEmpty then
          battingLogTable stats.BattingLog
      match stats.Fielding with
      | Some f -> fieldingTable f
      | None -> ()
      if not stats.FieldingLog.IsEmpty then
          fieldingLogTable stats.FieldingLog
      match stats.Pitching with
      | Some p -> pitchingTable p
      | None -> ()
      if not stats.PitchingLog.IsEmpty then
          pitchingLogTable stats.PitchingLog
      
      if stats.Batting.IsNone && stats.Fielding.IsNone && stats.Pitching.IsNone then
          p [ _class "mt-8 rounded-lg bg-card p-6 text-ink-muted ring-1 ring-card-ring" ] [ str "No season stats yet." ] ]
