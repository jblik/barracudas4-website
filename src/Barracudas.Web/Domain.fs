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
      OpponentScore: int option }

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

type PlayerStat =
    { Id: string
      Name: string
      Number: int option
      Position: string
      Games: int
      AtBats: int
      Runs: int
      Hits: int
      HomeRuns: int
      Rbi: int
      /// Batting average, 0.0–1.0.
      Avg: float }

/// In-progress game snapshot for the live banner.
type LiveGame =
    { Opponent: string
      IsHome: bool
      OurScore: int
      OpponentScore: int
      /// Current inning number.
      Inning: int
      /// True = top of the inning, false = bottom.
      IsTop: bool
      Outs: int }
