module Barracudas.Web.EasyScore.SwissBaseball

open System
open System.Globalization
open System.Net.Http
open System.Text.RegularExpressions
open System.Threading.Tasks
open HtmlAgilityPack
open Barracudas.Web
open Barracudas.Web.Domain
open Barracudas.Web.EasyScore.Client

/// Reads the public swiss-baseball.ch league feed — the same `admin-ajax`
/// `bsm_show_leagues` endpoint the federation site uses — and parses the
/// standings + games HTML for our league (1. Liga Ost, index 160).
///
/// No EasyScore API key required. Live in-progress scores are NOT available
/// here (they live in EasyScore's Firestore), so GetLiveGame returns None and
/// per-player season stats aren't published by this feed (GetPlayers = []).
type SwissBaseballClient(http: HttpClient, cfg: Config.AppConfig) =
    let inv = CultureInfo.InvariantCulture

    let isUs (name: string) (abbr: string) =
        name.Contains("Barracudas", StringComparison.OrdinalIgnoreCase)
        || (abbr <> "" && abbr.Equals(cfg.TeamAbbr, StringComparison.OrdinalIgnoreCase))

    let fetchHtml () : Task<string> =
        task {
            try
                use content =
                    new FormUrlEncodedContent(
                        dict [ "action", "bsm_show_leagues"
                               "dataType", "html"
                               "league_id", string cfg.LeagueIndex ])
                let! resp = http.PostAsync(cfg.SourceUrl, content)
                resp.EnsureSuccessStatusCode() |> ignore
                return! resp.Content.ReadAsStringAsync()
            with _ ->
                return ""  // degrade gracefully: callers see empty data
        }

    let loadDoc (html: string) =
        let doc = HtmlDocument()
        doc.LoadHtml(html)
        doc

    let txt (node: HtmlNode) =
        if isNull node then "" else HtmlEntity.DeEntitize(node.InnerText).Trim()

    let cell (row: HtmlNode) (name: string) =
        txt (row.SelectSingleNode(sprintf ".//*[@data-cell-for='%s']" name))

    let intOf (s: string) = match Int32.TryParse s with | true, v -> v | _ -> 0

    /// PCT like "1.000" or ".667" → float.
    let parsePct (s: string) =
        let s = if s.StartsWith "." then "0" + s else s
        match Double.TryParse(s, NumberStyles.Any, inv) with true, v -> v | _ -> 0.0

    let parseStandings (doc: HtmlDocument) : Standing list =
        match doc.DocumentNode.SelectNodes("//tr[td[@data-cell-for='rank']]") with
        | null -> []
        | rows ->
            [ for r in rows ->
                let name = cell r "team"
                let abbr = cell r "acr"
                { Rank = intOf ((cell r "rank").TrimEnd('.'))
                  Team = name
                  Abbr = abbr
                  Games = intOf (cell r "games")
                  Wins = intOf (cell r "wins")
                  Losses = intOf (cell r "losses")
                  Pct = parsePct (cell r "pct")
                  GamesBehind =
                    (match Double.TryParse((cell r "gamesback").Replace(",", "."), NumberStyles.Any, inv) with
                     | true, v -> v
                     | _ -> 0.0)
                  Streak = cell r "streak"
                  IsUs = isUs name abbr } ]

    let parseGames (doc: HtmlDocument) (season: int) : Game list =
        match doc.DocumentNode.SelectNodes("//tr[@data-row-for]") with
        | null -> []
        | rows ->
            let games =
                [ for r in rows do
                    let away, awayAbbr = cell r "away", cell r "awayAbbr"
                    let home, homeAbbr = cell r "home", cell r "homeAbbr"
                    let weAreHome = isUs home homeAbbr
                    let weAreAway = isUs away awayAbbr
                    if weAreHome || weAreAway then
                        // Date comes from the enclosing table's data-grouping ("Sa. 25.04.2026").
                        let grouping =
                            let t = r.SelectSingleNode("ancestor::table[@data-grouping]")
                            if isNull t then "" else t.GetAttributeValue("data-grouping", "")
                        let date =
                            let m = Regex.Match(grouping, @"\d{2}\.\d{2}\.\d{4}")
                            let baseDate =
                                if m.Success then
                                    match DateTime.TryParseExact(m.Value, "dd.MM.yyyy", inv, DateTimeStyles.None) with
                                    | true, d -> d
                                    | _ -> DateTime(season, 1, 1)
                                else DateTime(season, 1, 1)
                            match TimeSpan.TryParseExact(cell r "time", "hh\\:mm", inv) with
                            | true, ts -> baseDate.Add ts
                            | _ -> baseDate
                        // Result is "away-home", e.g. "7-13".
                        let scores =
                            let parts = (cell r "result").Split('-')
                            if parts.Length = 2 then
                                match Int32.TryParse(parts.[0].Trim()), Int32.TryParse(parts.[1].Trim()) with
                                | (true, a), (true, h) -> Some(a, h)
                                | _ -> None
                            else None
                        let esId =
                            let v = r.GetAttributeValue("data-row-for", "")
                            if v.StartsWith "ES" then v.Substring 2 else v
                        let boxHref =
                            let a = r.SelectSingleNode(".//td[@data-cell-for='result']//a")
                            if isNull a then "" else a.GetAttributeValue("href", "")
                        let status = if scores.IsSome then Final else Scheduled
                        let ourScore, oppScore =
                            match scores with
                            | Some(a, h) -> (if weAreHome then Some h, Some a else Some a, Some h)
                            | None -> None, None
                        yield
                            { Id = (if esId = "" then Guid.NewGuid().ToString("N").Substring(0, 6) else esId)
                              Date = date
                              Opponent = (if weAreHome then away else home)
                              IsHome = weAreHome
                              Location = cell r "field"
                              Status = status
                              OurScore = ourScore
                              OpponentScore = oppScore
                              EasyScoreId = (if esId = "" then None else Some esId)
                              BoxScoreUrl = (if status = Final && boxHref <> "" then Some boxHref else None) } ]
            games |> List.sortBy (fun g -> g.Date)

    let computeTeamStats (standings: Standing list) (games: Game list) : TeamStat list =
        let finals = games |> List.filter (fun g -> g.Status = Final)
        let rs = finals |> List.sumBy (fun g -> defaultArg g.OurScore 0)
        let ra = finals |> List.sumBy (fun g -> defaultArg g.OpponentScore 0)
        let diff = rs - ra
        [ match standings |> List.tryFind (fun s -> s.IsUs) with
          | Some s ->
              { Label = "Record"; Value = sprintf "%d–%d" s.Wins s.Losses }
              { Label = "Win %"; Value = (let p = s.Pct.ToString("0.000", inv) in if p.StartsWith "0" then p.Substring 1 else p) }
              { Label = "Streak"; Value = s.Streak }
          | None -> ()
          { Label = "Runs Scored"; Value = string rs }
          { Label = "Runs Allowed"; Value = string ra }
          { Label = "Run Diff"; Value = (if diff >= 0 then sprintf "+%d" diff else string diff) } ]

    interface IEasyScoreClient with
        member _.GetSchedule(season) =
            task {
                let! html = fetchHtml ()
                return parseGames (loadDoc html) season
            }
        member _.GetStandings() =
            task {
                let! html = fetchHtml ()
                return parseStandings (loadDoc html)
            }
        member _.GetTeamStats() =
            task {
                let! html = fetchHtml ()
                let doc = loadDoc html
                return computeTeamStats (parseStandings doc) (parseGames doc cfg.Season)
            }
        // Not available from the league feed (would need EasyScore API / boxscore aggregation).
        member _.GetPlayers() = Task.FromResult []
        member _.GetPlayer(_id) = Task.FromResult None
        // Live in-progress data lives in EasyScore's Firestore, not this feed.
        member _.GetLiveGame() = Task.FromResult None
