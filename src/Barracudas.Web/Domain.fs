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
      /// On-site box score URL ("/boxscore/{id}") for a completed game, when available.
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
    /// Roster-list form: "LASTNAME, First Name".
    member p.ListName = sprintf "%s, %s" (p.LastName.ToUpperInvariant()) p.FirstName

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

/// One game's batting line from the player's game log (newest first).
type BattingLogEntry =
    { Date: DateTime
      Opponent: string
      /// Batting-order spot, e.g. "4".
      Spot: string
      /// Positions played, e.g. "DH,P,SS".
      Pos: string
      AB: string
      R: string
      H: string
      Doubles: string
      Triples: string
      HR: string
      RBI: string
      BB: string
      SO: string
      SB: string
      CS: string
      HBP: string
      Sac: string
      SacFlies: string
      GIDP: string
      TwoOutRBI: string
      /// Hits with runners in scoring position, e.g. "1/3".
      RISP: string
      GameScore: string
      /// Season batting average through this game.
      AvgToDate: string }

/// One game's fielding line from the player's game log (newest first).
type FieldingLogEntry =
    { Date: DateTime
      Opponent: string
      Pos: string
      Innings: string
      Putouts: string
      Assists: string
      OutfieldAssists: string
      Errors: string
      DoublePlays: string
      PassedBalls: string
      StealAttempts: string
      CaughtStealing: string
      /// Season range factor through this game.
      RangeFactorToDate: string
      /// Season fielding percentage through this game.
      FieldingPctToDate: string }

/// One game's pitching line from the player's game log (newest first).
type PitchingLogEntry =
    { Date: DateTime
      Opponent: string
      IP: string
      H: string
      R: string
      ER: string
      BB: string
      SO: string
      HBP: string
      WildPitches: string
      Balks: string
      GroundBalls: string
      FlyBalls: string
      BattersFaced: string
      Pitches: string
      /// Decision, e.g. "W", "L", "SV" or "--".
      Decision: string
      /// Relief decision (hold/blown save) or "--".
      Relief: string
      GameScore: string
      /// Season WHIP through this game.
      WhipToDate: string
      /// Season ERA through this game.
      EraToDate: string }

/// All season stats of one player; a section is None when the player has no
/// appearances in that category. The logs hold one entry per game, newest first.
type PlayerStats =
    { Batting: BattingStats option
      Fielding: FieldingStats option
      Pitching: PitchingStats option
      BattingLog: BattingLogEntry list
      FieldingLog: FieldingLogEntry list
      PitchingLog: PitchingLogEntry list }

/// One team's row in the inning-by-inning linescore.
type LineScoreTeam =
    { Name: string
      Abbr: string
      Logo: string option
      /// Runs scored in each inning, in order ("x" when a side didn't bat).
      Innings: string list
      Runs: int
      Hits: int
      Errors: int }

/// The inning-by-inning linescore grid (R/H/E totals per side).
type LineScore =
    { Innings: int
      Away: LineScoreTeam
      Home: LineScoreTeam }

/// One batter's line in a box score. The team totals row has Order = None.
type BoxBatter =
    { /// Batting-order spot (None for the totals row).
      Order: int option
      /// True for a substitute (indented under the player they replaced).
      IsSub: bool
      Pos: string
      /// "Lastname Firstname", as scored.
      Name: string
      AB: int
      R: int
      H: int
      RBI: int
      BB: int
      SO: int
      LOB: int
      /// Season batting average through this game ("" → no at-bats yet).
      Avg: string }

/// One pitcher's line in a box score.
type BoxPitcher =
    { /// "Lastname Firstname", with any decision, e.g. "Würsten Samuel (L, 0-1)".
      Name: string
      /// True for the team totals row.
      IsTotals: bool
      IP: string
      H: int
      R: int
      ER: int
      BB: int
      SO: int
      HR: int
      BattersFaced: int
      Pitches: int
      Strikes: int
      /// Season ERA through this game.
      ERA: string }

/// One side's batting + pitching lines in a box score. Batters end with the
/// team totals row (Order = None).
type BoxTeam =
    { Name: string
      Abbr: string
      Logo: string option
      /// True for our own team (highlighted).
      IsUs: bool
      Batters: BoxBatter list
      Pitchers: BoxPitcher list }

/// A labelled game note (e.g. "HR", "2B", "E") with the away/home detail text.
type BoxNote =
    { Label: string
      Away: string option
      Home: string option }

/// A completed game's full box score (header + linescore + both teams + notes).
type BoxScore =
    { GameId: string
      Date: DateTime
      Location: string
      Round: string
      Umpires: string
      Scorer: string
      LineScore: LineScore option
      Away: BoxTeam
      Home: BoxTeam
      /// Game notes (doubles, home runs, errors, …); empty entries omitted.
      Notes: BoxNote list }

/// In-progress game for the live banner (rendered via the EasyScore
/// linescore overlay, https://www.easyscore.com/overlays/linescores/{id}).
type LiveGame =
    { /// EasyScore game id, e.g. "19313".
      GameId: string
      AwayName: string
      HomeName: string }
