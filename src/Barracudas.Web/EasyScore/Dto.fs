module Barracudas.Web.EasyScore.Dto

open System.Text.Json
open System.Text.Json.Serialization

/// Raw JSON shapes from api.easyscore.com/v2, exactly as served.
/// Dates/times stay strings here — all parsing happens in Convert.
/// GET /rounds?byLeague=1&lg={leagueId}
type RoundDto =
    { ID: int
      Round: string
      League: string }

/// GET /teams?byRound={roundId}&lg={leagueId}&yr={year}
type TeamDto = { Team: int; Name: string }

/// GET /teams?id={teamId} — full team record (only the brand colour is used).
type TeamDetailDto =
    { ID: int
      Name: string
      MainColor: string option }

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
      Team: int option
      Pos: string option
      G: int option
      PA: string option
      AB: string option
      R: string option
      H: string option
      RBI: string option
      ``2B``: string option
      ``3B``: string option
      HR: string option
      TB: string option
      BB: string option
      SO: string option
      HBP: string option
      SB: string option
      CS: string option
      BA: string option
      OBP: string option
      SLG: string option
      OPS: string option }

/// GET /stats?…&cat=fld
type FieldingStatsDto =
    { PlayerID: int
      Team: int
      G: int option
      InningsPlayed: string option
      Putout: string option
      Assist: string option
      [<JsonPropertyName "Outfield Assists">]
      OutfieldAssists: string option
      Error: string option
      DP: string option
      PB: string option
      SBAtt: string option
      CSMade: string option
      RangeFactor: string option
      FPct: string option }

/// GET /stats?…&cat=pit (note: G is a string here, unlike off/fld)
type PitchingStatsDto =
    { PlayerID: int
      Team: int
      G: string option
      GS: string option
      IP: string option
      HA: string option
      RA: string option
      ER: string option
      BBA: string option
      K: string option
      HBPA: string option
      WP: string option
      W: string option
      L: string option
      SV: string option
      BF: string option
      OppAVG: string option
      WHIP: string option
      ERA: string option }

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
      ``2B``: string
      ``3B``: string
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

/// One side of a linescore entry (GET /games?id=…). `line` is keyed by inning
/// number; values are ints, or the string "x" when the side didn't bat.
type LineScoreSideDto =
    { [<JsonPropertyName "abbr">]
      TeamAbbreviation: string
      [<JsonPropertyName "team">]
      TeamName: string
      logo: string option
      line: Map<string, JsonElement>
      totals: LineScoreTotalsDto }

and LineScoreTotalsDto = { R: int; H: int; E: int }

/// One game's linescore grid (a single-element list in the response).
type LineScoreEntryDto =
    { away: LineScoreSideDto
      home: LineScoreSideDto
      innings: int }

/// GET /games?id={gameId}&… — only the linescore is consumed here; the rest of
/// the box score comes from /stats?box.
type GameDetailDto =
    { ID: int
      AwayTeam: int
      HomeTeam: int
      LineScore: LineScoreEntryDto list }

/// One batter's line in a box score (GET /stats?box={gameId}). The team totals
/// row has Spot = 100 and an empty name.
type BoxHitterDto =
    { Spot: int
      SubbedIn: string option
      TopOrBot: string
      Pos: string
      playerID: int option
      playerName: string
      AB: int option
      R: int option
      H: int option
      RBI: int option
      BB: int option
      SO: int option
      LOB: int option
      BA: string option }

/// One pitcher's line in a box score (GET /stats?box). HA/RA/HRA = hits/runs/HR
/// allowed; BBA = walks allowed; K = strikeouts.
type BoxPitcherDto =
    { PitcherNr: int
      TopOrBot: string
      playerName: string
      IP: string
      HA: int
      RA: int
      ER: int
      BBA: int
      K: int
      HRA: int
      BF: int
      PitchCount: int
      Strikes: int
      ERA: string }

/// A game note keyed by team side (T = away, B = home); either may be absent.
type BoxNoteDto = { T: string option; B: string option }

/// The box score payload (GET /stats?box={gameId} → [{ BoxScores }]).
type BoxScoresDto =
    { AwayTeam: string
      HomeTeam: string
      AwayTeamAbbr: string
      HomeTeamAbbr: string
      AwayTeamLogo: string option
      HomeTeamLogo: string option
      GameInfo: BoxGameInfoDto
      AllHitters: BoxHitterDto list
      AllPitchers: BoxPitcherDto list
      AdditionalBatting2B: BoxNoteDto option
      AdditionalBatting3B: BoxNoteDto option
      AdditionalBattingHR: BoxNoteDto option
      AdditionalBattingSF: BoxNoteDto option
      AdditionalBattingGIDP: BoxNoteDto option
      AdditionalBaserunningSB: BoxNoteDto option
      AdditionalBaserunningCS: BoxNoteDto option
      AdditionalFieldingError: BoxNoteDto option
      AdditionalFieldingDPs: BoxNoteDto option
      AdditionalFieldingPB: BoxNoteDto option
      AdditionalPitchingWP: BoxNoteDto option
      AdditionalPitchingHBP: BoxNoteDto option
      AdditionalPitchingBalk: BoxNoteDto option }

and BoxGameInfoDto =
    { Date: string
      Round: string
      Misc: BoxMiscDto }

and BoxMiscDto =
    { Field: string
      Umpires: string
      Scorer: string }

/// GET /stats?box — the response is a single-element list wrapping BoxScores.
type BoxScoreResponseDto = { BoxScores: BoxScoresDto }

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
