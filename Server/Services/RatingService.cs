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
        private const int MaximumRatingChange = 55;
        private const int BaseRatingChange = 30;

        public RatingService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<LeaderboardPlayer>> GetLeaderboard()
        {
            return await _context.Players
                            .OrderByDescending(p => p.Rating12v12)
                            .Take(NumberOfTopPlayers)
                            .Select(p => new LeaderboardPlayer { SteamId = p.SteamId.ToString(), Rating = p.Rating12v12 })
                            .ToListAsync();
        }

        public Dictionary<ulong, PlayerRatingChange> RecordRankedMatch(IEnumerable<MatchPlayer> matchPlayers, ushort winnerTeam)
        {
            var teams = SplitTeams(matchPlayers, winnerTeam);

            var averageWinningRating = teams.Where(x => x.Value == GameResult.Winner).Average(p => p.Key.Rating12v12);
            var averageLosingRating = teams.Where(x => x.Value == GameResult.Loser).Average(p => p.Key.Rating12v12);
            // Formula is the difference between loosing and winning team of avg rating divided by 40
            var scoreDelta = CalculateScoreDelta(averageWinningRating, averageLosingRating);

            return GetPlayersChange(teams, scoreDelta);
        }

        private int CalculateScoreDelta(double averageWinningRating, double averageLosingRating)
        {
            // e.g. -(1900 - 2100) / 40 = 5. Meaning winning team will get 5 more points 
            // than the base as their average was weaker
            var scoreDeltaDouble = -(averageWinningRating - averageLosingRating) / 40;
            var scoreDelta = (int)(Math.Round(scoreDeltaDouble, 0, MidpointRounding.AwayFromZero));
            return Math.Min(BaseRatingChange + scoreDelta, MaximumRatingChange);
        }

        private Dictionary<ulong, PlayerRatingChange> GetPlayersChange(Dictionary<Player, GameResult> teams, int scoreDelta)
        {
            var result = new Dictionary<ulong, PlayerRatingChange>();
            foreach (var playerTeam in teams)
            {
                var playerChange = new PlayerRatingChange() { Old = playerTeam.Key.Rating12v12 };
                if (playerTeam.Value == GameResult.Winner)
                    playerTeam.Key.Rating12v12 += scoreDelta;
                else
                    playerTeam.Key.Rating12v12 = Math.Max(playerTeam.Key.Rating12v12 - scoreDelta, 0);
                playerChange.New = playerTeam.Key.Rating12v12;
                result.Add(playerTeam.Key.SteamId, playerChange);
            }

            return result;
        }

        private Dictionary<Player, GameResult> SplitTeams(IEnumerable<MatchPlayer> matchPlayers, ushort winnerTeam)
        {
            var result = new Dictionary<Player, GameResult>();
            foreach (var matchPlayer in matchPlayers)
                result.Add(matchPlayer.Player, matchPlayer.Team == winnerTeam ? GameResult.Winner : GameResult.Loser);
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
        public int Rating { get; set; }
    }

    public class PlayerRatingChange
    {
        public int Old { get; set; }
        public int New { get; set; }
    }
}
