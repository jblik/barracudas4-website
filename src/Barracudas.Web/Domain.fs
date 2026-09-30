module Barracudas.Web.Domain

open System

type GameStatus =
    | Scheduled
    | Live
    | Final

/// One side of a matchup (logo URL served by EasyScore/BSM).
type GameTeam = { Name: string; Logo: string option }

type Game =
    { Id: string
      Date: DateTime
      Away: GameTeam
      Home: GameTeam
      /// True when the Barracudas are the home team.
      IsHome: bool
      Location: string
      Status: GameStatus
      /// Scores (None until the game has data).
      AwayScore: int option
      HomeScore: int option
      /// Box score URL for a completed game (EasyScore), when available.
      BoxScoreUrl: string option }
    /// Barracudas score for this game, when known.
    member g.OurScore = if g.IsHome then g.HomeScore else g.AwayScore
    member g.OpponentScore = if g.IsHome then g.AwayScore else g.HomeScore

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

/// Roster entry from the EasyScore licence registry.
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
      /// Season batting average, pre-formatted (".294"); None without at-bats.
      BattingAvg: string option }
    member p.Name = sprintf "%s %s" p.FirstName p.LastName

/// Season batting line (values pre-formatted by EasyScore).
type BattingStats =
    { Games: int
      PA: string
      AB: string
      R: string
      H: string
      Doubles: string
      Triples: string
      HR: string
      RBI: string
      TB: string
      BB: string
      SO: string
      HBP: string
      SB: string
      CS: string
      AVG: string
      OBP: string
      SLG: string
      OPS: string }

/// Season fielding line.
type FieldingStats =
    { Games: int
      Innings: string
      Putouts: string
      Assists: string
      OutfieldAssists: string
      Errors: string
      DoublePlays: string
      PassedBalls: string
      StealAttempts: string
      CaughtStealing: string
      RangeFactor: string
      FieldingPct: string }

/// Season pitching line.
type PitchingStats =
    { Games: string
      Starts: string
      IP: string
      H: string
      R: string
      ER: string
      BB: string
      SO: string
      HBP: string
      WildPitches: string
      Record: string
      Saves: string
      BattersFaced: string
      OppAVG: string
      WHIP: string
      ERA: string }

/// All season stats of one player; a section is None when the player has no
/// appearances in that category.
type PlayerStats =
    { Batting: BattingStats option
      Fielding: FieldingStats option
      Pitching: PitchingStats option }

/// In-progress game for the live banner (rendered via the EasyScore
/// linescore overlay, https://www.easyscore.com/overlays/linescores/{id}).
type LiveGame =
    { /// EasyScore game id, e.g. "19313".
      GameId: string
      AwayName: string
      HomeName: string }
