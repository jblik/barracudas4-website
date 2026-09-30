module Barracudas.Web.Config

open Microsoft.Extensions.Configuration

/// Application configuration, bound from IConfiguration (appsettings + env + user-secrets).
/// ApiKey comes from the EasyScore__ApiKey environment variable in production
/// (appsettings.Development.json in dev) — never from the committed appsettings.json.
type AppConfig =
    { /// EasyScore API base URL (e.g. https://api.easyscore.com/v2).
      BaseUrl: string
      /// EasyScore API key, sent as the x-api-key header.
      ApiKey: string
      /// EasyScore user id (uid) that scopes the /players endpoint.
      RequestUserId: string
      /// EasyScore league id for 1. Liga Baseball (2026 = 10143).
      LeagueId: int
      /// Substring that picks our round out of the league's rounds ("Ost").
      RoundFilter: string
      /// EasyScore team id for Zürich Barracudas 4 (13069).
      TeamId: int
      /// EasyScore player ids of the active roster (top-level ActiveRoster array).
      ActiveRoster: int list
      /// Current season (year).
      Season: int
      /// Live banner poll interval in seconds.
      LivePollSeconds: int }

let load (cfg: IConfiguration) : AppConfig =
    let section = cfg.GetSection "EasyScore"
    let str key fallback =
        match section.[key] with
        | null | "" -> fallback
        | v -> v
    let intOr key fallback =
        match System.Int32.TryParse(section.[key]) with
        | true, v -> v
        | _ -> fallback
    { BaseUrl = str "BaseUrl" "https://api.easyscore.com/v2"
      ApiKey = str "ApiKey" ""
      RequestUserId = str "RequestUserId" ""
      LeagueId = intOr "LeagueId" 10143
      RoundFilter = str "RoundFilter" "Ost"
      TeamId = intOr "TeamId" 13069
      ActiveRoster =
        cfg.GetSection("ActiveRoster").GetChildren()
        |> Seq.choose (fun c ->
            match System.Int32.TryParse c.Value with
            | true, v -> Some v
            | _ -> None)
        |> List.ofSeq
      Season = intOr "Season" System.DateTime.Now.Year
      LivePollSeconds = intOr "LivePollSeconds" 25 }
