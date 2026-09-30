module Barracudas.Web.EasyScore.Mock

open System
open System.Threading.Tasks
open Barracudas.Web.Domain
open Barracudas.Web.EasyScore.Client

// Fixture data approximating a Swiss 1. Liga season for the Barracudas.
// Shapes mirror Domain types so the real client can drop in unchanged.

let private opponents =
    [ "Zürich Challengers"; "Wil Pirates"; "Bern Cardinals"
      "Luzern Hawks"; "Therwil Flyers"; "Bern Devils" ]

let private games : Game list =
    let year = DateTime.Now.Year
    [ { Id = "g1"; Date = DateTime(year, 4, 13); Opponent = "Zürich Challengers"; IsHome = true;  Location = "Heerenschürli, Zürich"; Status = Final;     OurScore = Some 7; OpponentScore = Some 3 }
      { Id = "g2"; Date = DateTime(year, 4, 27); Opponent = "Wil Pirates";        IsHome = false; Location = "Bergholz, Wil";          Status = Final;     OurScore = Some 5; OpponentScore = Some 8 }
      { Id = "g3"; Date = DateTime(year, 5, 11); Opponent = "Bern Cardinals";     IsHome = true;  Location = "Heerenschürli, Zürich"; Status = Final;     OurScore = Some 10; OpponentScore = Some 2 }
      { Id = "g4"; Date = DateTime(year, 5, 25); Opponent = "Luzern Hawks";       IsHome = false; Location = "Allmend, Luzern";       Status = Final;     OurScore = Some 6; OpponentScore = Some 6 }
      { Id = "g5"; Date = DateTime(year, 6, 8);  Opponent = "Therwil Flyers";     IsHome = true;  Location = "Heerenschürli, Zürich"; Status = Live;      OurScore = Some 2; OpponentScore = Some 1 }
      { Id = "g6"; Date = DateTime(year, 6, 22); Opponent = "Bern Devils";        IsHome = false; Location = "Allmend, Bern";        Status = Scheduled; OurScore = None;   OpponentScore = None }
      { Id = "g7"; Date = DateTime(year, 7, 6);  Opponent = "Zürich Challengers"; IsHome = false; Location = "Heerenschürli, Zürich"; Status = Scheduled; OurScore = None;   OpponentScore = None }
      { Id = "g8"; Date = DateTime(year, 7, 20); Opponent = "Wil Pirates";        IsHome = true;  Location = "Heerenschürli, Zürich"; Status = Scheduled; OurScore = None;   OpponentScore = None } ]

// 1. Liga Baseball Ost (swiss-baseball.ch league index 160) — current table.
let private standings : Standing list =
    [ { Rank = 1; Team = "Wittenbach-St.Gallen"; Abbr = "WIT";      Games = 6; Wins = 6; Losses = 0; Pct = 1.000; GamesBehind = 0.0; Streak = "W6"; IsUs = false }
      { Rank = 2; Team = "Zürich Barracudas 4";  Abbr = "BAR4";     Games = 6; Wins = 4; Losses = 2; Pct = 0.667; GamesBehind = 2.0; Streak = "W2"; IsUs = true }
      { Rank = 3; Team = "Embrach Mustangs 2";   Abbr = "BTE2";     Games = 6; Wins = 2; Losses = 4; Pct = 0.333; GamesBehind = 4.0; Streak = "W1"; IsUs = false }
      { Rank = 4; Team = "Wil Pirates 2";        Abbr = "DEV";      Games = 6; Wins = 2; Losses = 4; Pct = 0.333; GamesBehind = 4.0; Streak = "L4"; IsUs = false }
      { Rank = 5; Team = "Zürich Eighters";      Abbr = "EIG/LIO2"; Games = 6; Wins = 1; Losses = 5; Pct = 0.167; GamesBehind = 5.0; Streak = "L2"; IsUs = false } ]

let private teamStats : TeamStat list =
    [ { Label = "Record";       Value = "5–2" }
      { Label = "Team AVG";     Value = ".287" }
      { Label = "Runs Scored";  Value = "47" }
      { Label = "Runs Allowed"; Value = "28" }
      { Label = "Home Runs";    Value = "9" }
      { Label = "Team ERA";     Value = "3.42" }
      { Label = "Stolen Bases"; Value = "18" }
      { Label = "Fielding %";   Value = ".961" } ]

let private players : PlayerStat list =
    [ { Id = "p1"; Name = "Marco Brunner";   Number = Some 7;  Position = "SS"; Games = 7; AtBats = 28; Runs = 9; Hits = 11; HomeRuns = 2; Rbi = 8;  Avg = 0.393 }
      { Id = "p2"; Name = "Luca Meier";      Number = Some 24; Position = "CF"; Games = 7; AtBats = 26; Runs = 7; Hits = 9;  HomeRuns = 1; Rbi = 5;  Avg = 0.346 }
      { Id = "p3"; Name = "Tim Keller";      Number = Some 11; Position = "C";  Games = 7; AtBats = 25; Runs = 5; Hits = 8;  HomeRuns = 3; Rbi = 11; Avg = 0.320 }
      { Id = "p4"; Name = "Jonas Widmer";    Number = Some 3;  Position = "2B"; Games = 6; AtBats = 22; Runs = 6; Hits = 7;  HomeRuns = 0; Rbi = 3;  Avg = 0.318 }
      { Id = "p5"; Name = "David Frei";      Number = Some 18; Position = "1B"; Games = 7; AtBats = 27; Runs = 4; Hits = 8;  HomeRuns = 2; Rbi = 9;  Avg = 0.296 }
      { Id = "p6"; Name = "Samuel Graf";     Number = Some 9;  Position = "RF"; Games = 7; AtBats = 24; Runs = 5; Hits = 6;  HomeRuns = 1; Rbi = 6;  Avg = 0.250 }
      { Id = "p7"; Name = "Nico Bachmann";   Number = Some 33; Position = "3B"; Games = 6; AtBats = 21; Runs = 3; Hits = 5;  HomeRuns = 0; Rbi = 4;  Avg = 0.238 }
      { Id = "p8"; Name = "Elias Suter";     Number = Some 15; Position = "LF"; Games = 5; AtBats = 18; Runs = 2; Hits = 4;  HomeRuns = 0; Rbi = 2;  Avg = 0.222 }
      { Id = "p9"; Name = "Raphael Steiner"; Number = Some 21; Position = "P";  Games = 4; AtBats = 12; Runs = 1; Hits = 2;  HomeRuns = 0; Rbi = 1;  Avg = 0.167 } ]

let private liveGame : LiveGame option =
    Some { Opponent = "Therwil Flyers"; IsHome = true; OurScore = 2; OpponentScore = 1; Inning = 4; IsTop = false; Outs = 1 }

type MockEasyScoreClient() =
    interface IEasyScoreClient with
        member _.GetSchedule(_season) = Task.FromResult games
        member _.GetStandings() = Task.FromResult standings
        member _.GetTeamStats() = Task.FromResult teamStats
        member _.GetPlayers() = Task.FromResult players
        member _.GetPlayer(id) = Task.FromResult(players |> List.tryFind (fun p -> p.Id = id))
        member _.GetLiveGame() = Task.FromResult liveGame
