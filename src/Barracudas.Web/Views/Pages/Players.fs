module Barracudas.Web.Views.Pages.Players

open Giraffe.ViewEngine
open Barracudas.Web.Domain
open Barracudas.Web.EasyScore.Api
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
/// `Full` is the tooltip / popup label (empty for the always-on base columns).
type private Column =
    { Key: string
      Label: string
      Full: string
      HeaderClass: string
      Cell: Player -> XmlNode
      SortKey: Player -> SortKey }

// --- Always-on base columns (No. / Name / B-T) -------------------------------

let private baseColumns =
    [ { Key = "no"
        Label = "No."
        Full = ""
        HeaderClass = "pb-2 pr-4"
        Cell = fun p -> td [ _class "py-3 pr-4 font-bold text-accent-text" ] [ str (num p.Number) ]
        SortKey =
          fun p ->
              match p.Number with
              | Some n -> Num(float n)
              | None -> Missing }
      { Key = "name"
        Label = "Name"
        Full = ""
        HeaderClass = "pb-2 pr-4"
        Cell =
          fun p ->
              td
                  [ _class "py-3 pr-4 font-semibold text-ink-strong" ]
                  [ a [ _href (sprintf "/players/%s" p.Id); _class "hover:text-accent-text" ] [ str p.ListName ] ]
        SortKey = fun p -> Str p.ListName }
      { Key = "bt"
        Label = "B/T"
        Full = ""
        HeaderClass = "pb-2 pr-4 text-center"
        Cell = fun p -> td [ _class "py-3 pr-4 text-center" ] [ str (batsThrows p) ]
        SortKey =
          fun p ->
              match batsThrows p with
              | "—" -> Missing
              | s -> Str s } ]

// --- Addable season-stat columns (batting / fielding / pitching) -------------

/// One selectable stat column: its category (for the popup grouping), the short
/// header label, the full name (tooltip + popup text) and how to read its value
/// off a player (None when the player has no row in that category).
type private StatCol =
    { Key: string
      Category: StatCategory
      Label: string
      Full: string
      Value: Player -> string option }

let private bat key label full (v: BattingStats -> string) : StatCol =
    { Key = "b_" + key
      Category = Offense
      Label = label
      Full = full
      Value = fun p -> p.Batting |> Option.map v }

let private fld key label full (v: FieldingStats -> string) : StatCol =
    { Key = "f_" + key
      Category = Fielding
      Label = label
      Full = full
      Value = fun p -> p.Fielding |> Option.map v }

let private pit key label full (v: PitchingStats -> string) : StatCol =
    { Key = "p_" + key
      Category = Pitching
      Label = label
      Full = full
      Value = fun p -> p.Pitching |> Option.map v }

/// Every addable column, in the order they appear in the table and the popup.
let private statCatalog : StatCol list =
    [ bat "g" "G" "Games" (fun s -> string s.Games)
      bat "pa" "PA" "Plate Appearances" _.PlateAppearances
      bat "ab" "AB" "At Bats" _.AtBats
      bat "r" "R" "Runs" _.Runs
      bat "h" "H" "Hits" _.Hits
      bat "2b" "2B" "Doubles" _.Doubles
      bat "3b" "3B" "Triples" _.Triples
      bat "hr" "HR" "Home Runs" _.HomeRuns
      bat "rbi" "RBI" "Runs Batted In" _.RunsBattedIn
      bat "tb" "TB" "Total Bases" _.TotalBases
      bat "bb" "BB" "Walks (Base on Balls)" _.BaseOnBalls
      bat "so" "SO" "Strikeouts" _.Strikeouts
      bat "hbp" "HBP" "Hit by Pitch" _.HitByPitch
      bat "sb" "SB" "Stolen Bases" _.StolenBases
      bat "cs" "CS" "Caught Stealing" _.CaughtStealing
      bat "avg" "AVG" "Batting Average" _.BattingAverage
      bat "obp" "OBP" "On-Base Percentage" _.OnBasePercentage
      bat "slg" "SLG" "Slugging Percentage" _.Slugging
      bat "ops" "OPS" "On-Base Plus Slugging" _.OnBasePlusSlugging

      fld "g" "G" "Games" (fun s -> string s.Games)
      fld "ip" "IP" "Innings Played" _.Innings
      fld "po" "PO" "Putouts" _.Putouts
      fld "a" "A" "Assists" _.Assists
      fld "oa" "OA" "Outfield Assists" _.OutfieldAssists
      fld "e" "E" "Errors" _.Errors
      fld "dp" "DP" "Double Plays" _.DoublePlays
      fld "pb" "PB" "Passed Balls" _.PassedBalls
      fld "sba" "SB Att" "Stolen Base Attempts" _.StealAttempts
      fld "cs" "CS" "Caught Stealing" _.CaughtStealing
      fld "rf" "RF" "Range Factor" _.RangeFactor
      fld "fpct" "FPCT" "Fielding Percentage" _.FieldingPct

      pit "g" "G" "Games" _.Games
      pit "gs" "GS" "Games Started" _.Starts
      pit "ip" "IP" "Innings Pitched" _.InningsPitched
      pit "h" "H" "Hits Allowed" _.HitsAllowed
      pit "r" "R" "Runs Allowed" _.RunsAllowed
      pit "er" "ER" "Earned Runs" _.EarnedRuns
      pit "bb" "BB" "Walks (Base on Balls)" _.BaseOnBalls
      pit "so" "SO" "Strikeouts" _.Strikeouts
      pit "hbp" "HBP" "Hit Batters" _.HitBatters
      pit "wp" "WP" "Wild Pitches" _.WildPitches
      pit "wl" "W–L" "Win–Loss Record" _.Record
      pit "sv" "SV" "Saves" _.Saves
      pit "bf" "BF" "Batters Faced" _.BattersFaced
      pit "oppavg" "OPP AVG" "Opponent Batting Average" _.OpponentBattingAverage
      pit "whip" "WHIP" "Walks and Hits per Inning Pitched" _.WalksHitsPerInningPitched
      pit "era" "ERA" "Earned Run Average" _.EarnedRunAverage ]

/// Columns shown by default (matches the original roster: name + batting average).
let defaultCols = [ "b_avg" ]

/// Parse the ?cols= list; absent means the default selection (first page load).
let parseCols (s: string option) : string list =
    match s with
    | None -> defaultCols
    | Some v -> v.Split(',') |> Array.toList |> List.filter (fun k -> k <> "")

let private parseStat (v: string) =
    match System.Double.TryParse(v, System.Globalization.CultureInfo.InvariantCulture) with
    | true, n -> Num n
    | _ -> if v = "" || v = "—" then Missing else Str v

/// A stat column rendered as a sortable table column.
let private toColumn (sc: StatCol) : Column =
    { Key = sc.Key
      Label = sc.Label
      Full = sc.Full
      HeaderClass = "pb-2 px-3 text-center"
      Cell =
        fun p ->
            td
                [ _class "py-3 px-3 text-center font-semibold text-ink-strong"; _title sc.Full ]
                [ str (sc.Value p |> Option.defaultValue "—") ]
      SortKey =
        fun p ->
            match sc.Value p with
            | Some v -> parseStat v
            | None -> Missing }

/// Every possible column, used for resolving a sort key by name.
let private allColumns = baseColumns @ (statCatalog |> List.map toColumn)

/// Base columns plus the selected stat columns, in catalog order.
let private activeColumns (cols: string list) =
    let selected = Set.ofList cols
    baseColumns @ (statCatalog |> List.filter (fun s -> selected.Contains s.Key) |> List.map toColumn)

/// Players already arrive in the default order (last name, first name);
/// no/unknown sort key keeps it.
let private applySort (sort: string option) (desc: bool) (players: Player list) =
    match sort |> Option.bind (fun k -> allColumns |> List.tryFind (fun c -> c.Key = k)) with
    | None -> players
    | Some col ->
        let missing, present = players |> List.partition (fun p -> col.SortKey p = Missing)

        let sorted =
            if desc then
                present |> List.sortByDescending col.SortKey
            else
                present |> List.sortBy col.SortKey

        sorted @ missing

/// Shared query string for the column set + sort (no popup flag — that is only
/// relevant to the partial, never to the page URL pushed into the address bar).
let private queryStr (cols: string list) (sort: string option) (dir: string) =
    let sortPart =
        match sort with
        | Some s -> $"&sort=%s{s}"
        | None -> ""

    let colsPart = String.concat "," cols
    $"?cols=%s{colsPart}%s{sortPart}&dir=%s{dir}"

/// HTMX attributes for a state-changing link: fetch the swappable panel, while
/// pushing the equivalent page URL so the selection survives refresh and sharing.
let private hxLink (cols: string list) (sort: string option) (dir: string) (menu: bool) =
    let q = queryStr cols sort dir
    [ _hxGet ("/players/partial" + q + (if menu then "&menu=open" else ""))
      _hxPushUrl ("/players" + q)
      _hxTarget "#players-panel"
      _hxSwap "outerHTML" ]

/// Clicking a header sorts ascending; clicking the active column flips the direction.
/// Sorting preserves the current column set but always closes the column picker
/// popup (so a sort click never re-opens it).
let private header (cols: string list) (active: string option) (desc: bool) (col: Column) =
    let isActive = active = Some col.Key
    let nextDir = if isActive && not desc then "desc" else "asc"
    let arrow = if isActive then (if desc then " ▼" else " ▲") else ""

    let attrs =
        _class (col.HeaderClass + " cursor-pointer select-none hover:underline")
        :: hxLink cols (Some col.Key) nextDir false

    th (if col.Full <> "" then attrs @ [ _title col.Full ] else attrs) [ str (col.Label + arrow) ]

let private row (cols: string list) (p: Player) =
    tr
        [ _class "border-b border-line transition-colors hover:bg-row-hover" ]
        [ for c in activeColumns cols -> c.Cell p ]

// --- Column picker popup -----------------------------------------------------

/// One toggle chip inside the popup: clicking adds or removes its column, keeping
/// the popup open and the current sort. Selected chips are filled gold; the full
/// stat name shows as a hover tooltip (the "cursor overlay").
let private columnToggle (cols: string list) (sort: string option) (desc: bool) (sc: StatCol) =
    let selected = List.contains sc.Key cols

    let next =
        if selected then
            cols |> List.filter (fun k -> k <> sc.Key)
        else
            cols @ [ sc.Key ]

    let cls =
        if selected then
            "cursor-help rounded-md bg-barracuda-accent px-2 py-1 text-xs font-bold text-barracuda-dark ring-1 ring-barracuda-accent transition-colors"
        else
            "cursor-help rounded-md bg-card px-2 py-1 text-xs font-semibold text-ink-strong ring-1 ring-card-ring transition-colors hover:ring-barracuda-accent/70"

    a
        ([ _class cls; _title sc.Full ] @ hxLink next sort (if desc then "desc" else "asc") true)
        [ str sc.Label ]

/// The "Columns" button + dropdown panel, grouped batting / fielding / pitching.
let private columnsMenu (cols: string list) (sort: string option) (desc: bool) (menu: bool) =
    let group (cat: StatCategory) =
        statCatalog
        |> List.filter (fun s -> s.Category = cat)
        |> List.map (columnToggle cols sort desc)

    let summaryAttrs =
        [ _class
              "inline-flex cursor-pointer select-none items-center gap-1.5 rounded-md bg-barracuda px-3 py-1.5 text-sm font-semibold text-white ring-1 ring-barracuda-accent/40 transition-colors hover:bg-barracuda-light" ]

    details
        (if menu then
             [ _class "relative inline-block"; attr "open" "open" ]
         else
             [ _class "relative inline-block" ])
        [ summary summaryAttrs [ span [ _class "text-base leading-none" ] [ str "⛁" ]; str "Columns" ]
          div
              [ _class
                    "absolute right-0 z-30 mt-2 w-72 max-w-[calc(100vw-2rem)] rounded-lg border border-barracuda-accent/50 bg-page-2 p-3 shadow-xl shadow-black/40" ]
              [ for cat in [ Offense; Fielding; Pitching ] do
                    yield
                        h3
                            [ _class
                                  "mt-3 mb-1.5 text-xs font-black uppercase tracking-wider text-accent-text first:mt-0" ]
                            [ str (StatCategory.toString cat) ]

                    yield div [ _class "flex flex-wrap gap-1.5" ] (group cat) ] ]

/// The swappable panel = column picker + roster table (returned by /players/partial).
let rosterPanel
    (cols: string list)
    (sort: string option)
    (desc: bool)
    (menu: bool)
    (players: Player list)
    : XmlNode =
    div
        [ _id "players-panel" ]
        [ div [ _class "mb-3 flex justify-end" ] [ columnsMenu cols sort desc menu ]
          div
              [ _class "overflow-x-auto" ]
              [ table
                    [ _class "w-full min-w-[24rem] text-left text-sm" ]
                    [ thead
                          [ _class
                                "border-b border-barracuda-accent/40 text-xs font-bold uppercase tracking-wider text-accent-text" ]
                          [ tr [] [ for c in activeColumns cols -> header cols sort desc c ] ]
                      tbody [] [ for p in applySort sort desc players -> row cols p ] ] ] ]

/// Full players page. Column set + sort come from the query string so a shared
/// or refreshed URL keeps the user's selection; the popup always starts closed.
let listView (cols: string list) (sort: string option) (desc: bool) (players: Player list) : XmlNode list =
    [ pageHeader "Players" "Team roster"
      if List.isEmpty players then
          p
              [ _class "rounded-lg bg-card p-6 text-ink-muted ring-1 ring-card-ring" ]
              [ str "The roster couldn't be loaded right now. Please check back later." ]
      else
          rosterPanel cols sort desc false players ]

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
          "PA", "Plate Appearances", s.PlateAppearances
          "AB", "At Bats", s.AtBats
          "R", "Runs", s.Runs
          "H", "Hits", s.Hits
          "2B", "Doubles", s.Doubles
          "3B", "Triples", s.Triples
          "HR", "Home Runs", s.HomeRuns
          "RBI", "Runs Batted In", s.RunsBattedIn
          "TB", "Total Bases", s.TotalBases
          "BB", "Walks (Base on Balls)", s.BaseOnBalls
          "SO", "Strikeouts", s.Strikeouts
          "HBP", "Hit by Pitch", s.HitByPitch
          "SB", "Stolen Bases", s.StolenBases
          "CS", "Caught Stealing", s.CaughtStealing
          "AVG", "Batting Average", s.BattingAverage
          "OBP", "On-Base Percentage", s.OnBasePercentage
          "SLG", "Slugging Percentage", s.Slugging
          "OPS", "On-Base Plus Slugging", s.OnBasePlusSlugging ]

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
          "IP", "Innings Pitched", s.InningsPitched
          "H", "Hits Allowed", s.HitsAllowed
          "R", "Runs Allowed", s.RunsAllowed
          "ER", "Earned Runs", s.EarnedRuns
          "BB", "Walks (Base on Balls)", s.BaseOnBalls
          "SO", "Strikeouts", s.Strikeouts
          "HBP", "Hit Batters", s.HitBatters
          "WP", "Wild Pitches", s.WildPitches
          "W–L", "Win–Loss Record", s.Record
          "SV", "Saves", s.Saves
          "BF", "Batters Faced", s.BattersFaced
          "OPP AVG", "Opponent Batting Average", s.OpponentBattingAverage
          "WHIP", "Walks and Hits per Inning Pitched", s.WalksHitsPerInningPitched
          "ERA", "Earned Run Average", s.EarnedRunAverage ]

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
          "Pos", "Positions Played", _.PositionsPlayed
          "AB", "At Bats", _.AtBats
          "R", "Runs", _.Runs
          "H", "Hits", _.Hits
          "2B", "Doubles", _.Doubles
          "3B", "Triples", _.Triples
          "HR", "Home Runs", _.HomeRuns
          "RBI", "Runs Batted In", _.RunsBattedIn
          "BB", "Walks (Base on Balls)", _.BaseOnBalls
          "SO", "Strikeouts", _.StrikeOuts
          "SB", "Stolen Bases", _.StolenBases
          "CS", "Caught Stealing", _.CaughtStealing
          "HBP", "Hit by Pitch", _.HitByPitches
          "S", "Sacrifice Bunts", _.SacrificeBunts
          "SF", "Sacrifice Flies", _.SacrificeFlies
          "GIDP", "Grounded into Double Plays", _.GroundedIntoDoublePlay
          "2-out RBI", "Two-Out Runs Batted In", _.TwoOutRBI
          "RISP", "Hits with Runners in Scoring Position", _.RunnersInScoringPosition
          "GSc", "Formula:\n59 + Hits + Runs + .25*Walks + .25*HitByPitch + TotalBases + .25*StolenBases - .25*CaughtStealing + .25*SacFlies + .25*SacHits + RBIs - .25*Strikeouts - .25*Outs", _.GameScore
          "AVG*", "Batting Average (season to date)", _.BattingAverageSeasonToDate ]
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
          "IP", "Innings Pitched", _.InningsPitched
          "H", "Hits Allowed", _.HitsAllowed
          "R", "Runs Allowed", _.RunsAllowed
          "ER", "Earned Runs", _.EarnedRuns
          "BB", "Walks (Base on Balls)", _.BaseOnBalls
          "SO", "Strikeouts", _.Strikeouts
          "HBP", "Hit Batters", _.HitBatters
          "WP", "Wild Pitches", _.WildPitches
          "BK", "Balks", _.Balks
          "GB", "Ground Balls", _.GroundBalls
          "FB", "Fly Balls", _.FlyBalls
          "BF", "Batters Faced", _.BattersFaced
          "#Pit", "Pitches Thrown", _.Pitches
          "Dec", "Decision (Win/Loss/Save)", _.Decision
          "Rel", "Relief Decision (Hold/Blown Save)", _.Relief
          "GSc", "Formula:\n50 + 1 point for each out recorded + 2 points for each inning completed after the 4th + 1 point for each strikeout - 2 points for each hit allowed - 4 points for each earned run allowed - 2 points for each unearned run allowed - 1 point for each walk.", _.GameScore
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
