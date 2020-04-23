using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Server.Models;

namespace Server.Services
{
    public class RatingService
    {
        private const string CacheKey = "Leaderboards";
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IMemoryCache _cache;

        public RatingService(AppDbContext context, IWebHostEnvironment environment, IMemoryCache cache)
        {
            _context = context;
            _environment = environment;
            _cache = cache;
        }

        public async Task<List<LeaderboardPlayer>> GetLeaderboard() =>
            await _cache.GetOrCreateAsync(CacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _environment.IsProduction() ? TimeSpan.FromMinutes(5) : TimeSpan.FromTicks(1);
                return await _context.Players
                    .OrderByDescending(p => p.Rating12v12)
                    .Take(100)
                    .Select(p => new LeaderboardPlayer { SteamId = p.SteamId.ToString(), Rating = p.Rating12v12 })
                    .ToListAsync();
            });

        public async Task<List<LeaderboardPlayer>> GetMapPlayersRating(List<ulong> players) =>
            await _cache.GetOrCreateAsync(CacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _environment.IsProduction() ? TimeSpan.FromMinutes(5) : TimeSpan.FromTicks(1);
                int rank = 1;
                return _context.Players
                    .OrderByDescending(p => p.Rating12v12)
                    .AsEnumerable()
                    .Select((p, i) => new LeaderboardPlayer { SteamId = p.SteamId.ToString(), Rating = p.Rating12v12, Rank = i + rank })
                    .Where(p => players.Contains(Convert.ToUInt64(p.SteamId)))
                    .ToList();
            });

        public Dictionary<ulong, PlayerRatingChange> RecordRankedMatch(IEnumerable<MatchPlayer> matchPlayers, ushort winnerTeam)
        {
            var teams = SplitTeams(matchPlayers, winnerTeam);

            var averageWinningRating = teams.Where(x => x.Value == GameResult.Winner).Average(p => p.Key.Rating12v12);
            var averageLosingRating = teams.Where(x => x.Value == GameResult.Loser).Average(p => p.Key.Rating12v12);
            // Formula is the difference between loosing and winning team of avg rating divided by 40
            var scoreDelta = CalculateScoreDelta(averageWinningRating, averageLosingRating);

            return GetPlayersChange(teams, scoreDelta);
        }

        private const int BaseRatingChange = 30;
        private const int MaximumRatingChange = 55;
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
        public int Rank { get; set; }
        public int Rating { get; set; }
    }

    public class PlayerRatingChange
    {
        public int Old { get; set; }
        public int New { get; set; }
    }
}
