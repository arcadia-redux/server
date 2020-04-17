using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Server.Models;

namespace Server.Services
{
    public class RatingService
    {
        private readonly AppDbContext _context;
        private const int NumberOfTopPlayers = 100;
        private const int MaximumDelta = 25;
        private const int BaseRating = 30;
        private const int DivisionPoints = 40;

        public RatingService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<LeaderboardPlayer>> GetLeaderboard()
        {
            return await _context.Players
                            .OrderByDescending(p => p.Rating12v12)
                            .Take(NumberOfTopPlayers)
                            .Select(p => new LeaderboardPlayer { SteamId = p.SteamId.ToString(), Rating = p.Rating12v12.ToString() })
                            .OrderByDescending(p => p.Rating)
                            .ToListAsync();
        }

        public Dictionary<ulong, PlayerRatingChange> RecordRankedMatch(IEnumerable<MatchPlayer> matchPlayers, ushort winnerTeam, List<Player> players)
        {
            var teams = SplitTeams(matchPlayers, winnerTeam, players);

            var averageWinningRating = teams.Where(x => x.Value == GameResult.Winner).Average(p => p.Key.Rating12v12);
            var averageLosingRating = teams.Where(x => x.Value == GameResult.Loser).Average(p => p.Key.Rating12v12);
            // Formula is the difference between loosing and winning team of avg rating divided by 40
            var scoreDelta = CalculateScoreDelta(averageWinningRating, averageLosingRating);

            return GetPlayersChange(teams, scoreDelta);
        }

        private int CalculateScoreDelta(double averageWinningRating, double averageLosingRating)
        {
            var scoreDeltaDouble =
                -(averageWinningRating - averageLosingRating) /
                DivisionPoints; // e.g. - (1900 - 2100) / 40 = 5. Meaning winning team will get 5 more points 
                                // than the base as their average was weaker
            var scoreDelta =
                Convert.ToInt32(Math.Round(scoreDeltaDouble, 0,
                    MidpointRounding.AwayFromZero)); // we don't do a conversion straight to the double
                                                     // as it will do a MidpointRounding.ToEven by default
            return Math.Min(scoreDelta, MaximumDelta);
        }

        private Dictionary<ulong, PlayerRatingChange> GetPlayersChange(Dictionary<Player, GameResult> teams, int scoreDelta)
        {
            var result = new Dictionary<ulong, PlayerRatingChange>();
            foreach (var playerTeam in teams)
            {
                var playerChange = new PlayerRatingChange() { Old = playerTeam.Key.Rating12v12.ToString() };
                if (playerTeam.Value == GameResult.Winner)
                    playerTeam.Key.Rating12v12 += BaseRating + scoreDelta;
                else
                    playerTeam.Key.Rating12v12  = playerTeam.Key.Rating12v12 < BaseRating + scoreDelta ? 0 : playerTeam.Key.Rating12v12 - (BaseRating + scoreDelta);
                playerChange.New = playerTeam.Key.Rating12v12.ToString();
                result.Add(playerTeam.Key.SteamId, playerChange);
            }

            return result;
        }

        private Dictionary<Player, GameResult> SplitTeams(IEnumerable<MatchPlayer> matchPlayers, ushort winnerTeam, List<Player> players)
        {
            var matchPlayersKvp = matchPlayers.ToDictionary(mp => mp.SteamId, mp => mp.Team);
            var result = new Dictionary<Player, GameResult>();
            foreach (var player in players)
                result.Add(player, matchPlayersKvp[player.SteamId] == winnerTeam ? GameResult.Winner : GameResult.Loser);
            return result;
        }
    }

    public enum GameResult
    {
        Winner,
        Loser
    }

    public class LeaderboardPlayer
    {
        public string SteamId { get; set; }
        public string Rating { get; set; }
    }

    public class PlayerRatingChange
    {
        public string Old { get; set; }
        public string New { get; set; }
    }
}
