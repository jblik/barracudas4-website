module Barracudas.Web.EasyScore.Dto

open System.Text.Json.Serialization

/// Raw JSON shapes from api.easyscore.com/v2, exactly as served.
/// Dates/times stay strings here — all parsing happens in Convert.

/// GET /rounds?byLeague=1&lg={leagueId}
type RoundDto =
    { ID: int
      Round: string
      League: string }

/// GET /teams?byRound={roundId}&lg={leagueId}&yr={year}
type TeamDto =
    { Team: int
      Name: string }

/// GET /schedule?yr={year}&lg={leagueId}&rd={roundId}
type GameDto =
    { ID: int
      GameDate: string
      StartTime: string option
      AwayTeam: int
      HomeTeam: int
      AwayTeamName: string
      HomeTeamName: string
      AwayTeamNameShort: string
      HomeTeamNameShort: string
      AwayLogo: string option
      HomeLogo: string option
      Field: string option
      GameEnded: int
      Live: int
      BoxScoreGenerated: int
      AwayRuns: int option
      HomeRuns: int option
      Forfeit: int
      postponed: int }

/// GET /stats?yr={year}&leagueID={leagueId}&round={roundId}&cat=off[&playerID={id}]
/// Stat values arrive pre-formatted as strings (".294", "10.50", "-.-").
type OffenseStatsDto =
    { PlayerID: int
      Team: int
      Pos: string option
      G: int
      PA: string
      AB: string
      R: string
      H: string
      RBI: string
      [<JsonPropertyName "2B">]
      Doubles: string
      [<JsonPropertyName "3B">]
      Triples: string
      HR: string
      TB: string
      BB: string
      SO: string
      HBP: string
      SB: string
      CS: string
      BA: string
      OBP: string
      SLG: string
      OPS: string }

/// GET /stats?…&cat=fld
type FieldingStatsDto =
    { PlayerID: int
      Team: int
      G: int
      InningsPlayed: string
      Putout: string
      Assist: string
      [<JsonPropertyName "Outfield Assists">]
      OutfieldAssists: string
      Error: string
      DP: string
      PB: string
      SBAtt: string
      CSMade: string
      RangeFactor: string
      FPct: string }

/// GET /stats?…&cat=pit (note: G is a string here, unlike off/fld)
type PitchingStatsDto =
    { PlayerID: int
      Team: int
      G: string
      GS: string
      IP: string
      HA: string
      RA: string
      ER: string
      BBA: string
      K: string
      HBPA: string
      WP: string
      W: string
      L: string
      SV: string
      BF: string
      OppAVG: string
      WHIP: string
      ERA: string }

/// GET /stats?…&cat=off&subCategory=log&playerID={id} — one row per game,
/// newest first. BA is the season average through that game.
type BattingLogDto =
    { GameID: int
      Date: string
      Opponent: string
      Spot: string
      Pos: string
      AB: string
      R: string
      H: string
      [<JsonPropertyName "2B">]
      Doubles: string
      [<JsonPropertyName "3B">]
      Triples: string
      HR: string
      RBI: string
      BB: string
      SO: string
      SB: string
      CS: string
      HBP: string
      S: string
      SF: string
      GIDP: string
      [<JsonPropertyName "2-out RBI">]
      TwoOutRBI: string
      RISP: string
      Gsc: string
      BA: string }

/// GET /stats?…&cat=fld&subCategory=log&playerID={id} —
/// RangeFactor/FPct are season values through that game.
type FieldingLogDto =
    { GameID: int
      Date: string
      Opponent: string
      Pos: string
      InningsPlayed: string
      Putout: string
      Assist: string
      [<JsonPropertyName "Outfield Assists">]
      OutfieldAssists: string
      Error: string
      DP: string
      PB: string
      SBAtt: string
      CSMade: string
      RangeFactor: string
      FPct: string }

/// GET /stats?…&cat=pit&subCategory=log&playerID={id} —
/// WHIP/ERA are season values through that game.
type PitchingLogDto =
    { GameID: int
      Date: string
      Opponent: string
      IP: string
      H: string
      R: string
      ER: string
      BB: string
      K: string
      HBP: string
      WP: string
      BK: string
      GB: string
      FB: string
      BF: string
      [<JsonPropertyName "#Pit">]
      Pitches: string
      [<JsonPropertyName "Dec.">]
      Decision: string
      [<JsonPropertyName "Rel.">]
      Relief: string
      GSc: string
      WHIP: string
      ERA: string }

/// GET /players?uid={userId}
type PlayerDto =
    { ID: int
      Lastname: string option
      Name: string option
      Team: string option
      UniformNr: string option
      Bats: string option
      Throws: string option
      Nationality: string option
      DateOfBirth: string option }
