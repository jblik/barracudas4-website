module Barracudas.Web.Config

open Microsoft.Extensions.Configuration

/// Application configuration, bound from IConfiguration (appsettings + env + user-secrets).
type AppConfig =
    { /// EasyScore API base URL (e.g. https://api.easyscore.com).
      BaseUrl: string
      /// EasyScore API key. Loaded from user-secrets / env — never committed.
      ApiKey: string
      /// Public swiss-baseball.ch admin-ajax endpoint that serves league data.
      SourceUrl: string
      /// swiss-baseball.ch league index for 1. Liga Baseball Ost.
      LeagueIndex: int
      /// EasyScore team identifier for the Barracudas 1. Liga side.
      TeamId: string
      /// Our team's short code in the league feed (e.g. "BAR4").
      TeamAbbr: string
      /// League name/identifier.
      League: string
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
    { BaseUrl = str "BaseUrl" "https://api.easyscore.com"
      ApiKey = str "ApiKey" ""
      SourceUrl = str "SourceUrl" "https://www.swiss-baseball.ch/wp-admin/admin-ajax.php"
      LeagueIndex = intOr "LeagueIndex" 160
      TeamId = str "TeamId" "barracudas4"
      TeamAbbr = str "TeamAbbr" "BAR4"
      League = str "League" "1. Liga Baseball Ost"
      Season = intOr "Season" System.DateTime.Now.Year
      LivePollSeconds = intOr "LivePollSeconds" 25 }
