module Barracudas.Web.Domain

open System

type GameStatus =
    | Scheduled
    | Live
    | Final

type Game =
    { Id: string
      Date: DateTime
      Opponent: string
      /// True when the Barracudas are the home team.
      IsHome: bool
      Location: string
      Status: GameStatus
      /// Barracudas score (None until the game has data).
      OurScore: int option
      OpponentScore: int option
      /// EasyScore game id (e.g. "19321"), when known.
      EasyScoreId: string option
      /// Box score URL for a completed game (EasyScore), when available.
      BoxScoreUrl: string option }

type Standing =
    { Rank: int
      Team: string
      /// Short team code shown by swiss-baseball.ch (e.g. "BAR4", "WIT").
      Abbr: string
      /// Games played.
      Games: int
      Wins: int
      Losses: int
      /// Winning percentage, 0.0–1.0.
      Pct: float
      /// Games behind the leader.
      GamesBehind: float
      /// Current win/loss streak, e.g. "W2" or "L4".
      Streak: string
      /// True for our own team (highlight row).
      IsUs: bool }

/// A single labelled team statistic (e.g. "Team AVG" -> ".287").
type TeamStat = { Label: string; Value: string }

/// Roster entry from the EasyScore licence registry (no per-player stats yet).
type Player =
    { Id: string
      FirstName: string
      LastName: string
      /// Uniform number, when registered.
      Number: int option
      /// Batting side: "R", "L", "S" or "".
      Bats: string
      /// Throwing arm: "R", "L" or "".
      Throws: string
      /// ISO country code, e.g. "CH".
      Nationality: string
      Age: int option }
    member p.Name = sprintf "%s %s" p.FirstName p.LastName

/// In-progress game for the live banner (rendered via the EasyScore
/// linescore overlay, https://www.easyscore.com/overlays/linescores/{id}).
type LiveGame =
    { /// EasyScore game id, e.g. "19313".
      GameId: string
      Opponent: string
      IsHome: bool }
