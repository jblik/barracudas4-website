module Barracudas.Web.EasyScore.Dto

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
      Field: string option
      GameEnded: int
      Live: int
      BoxScoreGenerated: int
      AwayRuns: int option
      HomeRuns: int option
      Forfeit: int
      postponed: int }

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
