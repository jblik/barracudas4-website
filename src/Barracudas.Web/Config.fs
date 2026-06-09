module Barracudas.Web.Config

open Microsoft.Extensions.Configuration

/// Application configuration, bound from IConfiguration (appsettings + env + user-secrets).
type AppConfig =
    { /// EasyScore API base URL (e.g. https://api.easyscore.com).
      BaseUrl: string
      /// EasyScore API key. Loaded from user-secrets / env — never committed.
      ApiKey: string
      /// When true, serve fixture data from EasyScore.Mock instead of hitting the API.
      UseMock: bool
      /// EasyScore team identifier for the Barracudas 1. Liga side.
      TeamId: string
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
    let boolOr key fallback =
        match bool.TryParse(section.[key]) with
        | true, v -> v
        | _ -> fallback
    let intOr key fallback =
        match System.Int32.TryParse(section.[key]) with
        | true, v -> v
        | _ -> fallback
    { BaseUrl = str "BaseUrl" "https://api.easyscore.com"
      ApiKey = str "ApiKey" ""
      UseMock = boolOr "UseMock" true
      TeamId = str "TeamId" "barracudas4"
      League = str "League" "1. Liga"
      Season = intOr "Season" System.DateTime.Now.Year
      LivePollSeconds = intOr "LivePollSeconds" 25 }
