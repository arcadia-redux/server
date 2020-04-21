using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

        public async Task<List<LeaderboardPlayer>> GetLeaderboard(CustomGame customGame, string mapName) =>
            await _cache.GetOrCreateAsync(CacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _environment.IsProduction() ? TimeSpan.FromMinutes(5) : TimeSpan.FromTicks(1);
                Expression<Func<Player, object>> orderQuery = p => customGame == CustomGame.Dota12v12 ? p.Rating12v12 : p.PlayerOverthrowRating.FirstOrDefault(x => x.MapName == mapName).Rating;
                return await _context.Players
                    .OrderByDescending(orderQuery)
                    .Take(100)
                    .Select(p => new LeaderboardPlayer { SteamId = p.SteamId.ToString(), Rating = customGame == CustomGame.Dota12v12 ? p.Rating12v12 : p.PlayerOverthrowRating.FirstOrDefault(x => x.MapName == mapName).Rating })
                    .ToListAsync();
            });

        #region 12v12 
        private const int BaseRatingChange12V12 = 30;
        public Dictionary<ulong, PlayerRatingChange> RecordRankedMatch12v12(IEnumerable<MatchPlayer> matchPlayers, ushort winnerTeam)
        {
            var teams = SplitTeams(matchPlayers, winnerTeam);

            var averageWinningRating = teams.Where(x => x.Value == GameResult.Winner).Average(p => p.Key.Rating12v12);
            var averageLosingRating = teams.Where(x => x.Value == GameResult.Loser).Average(p => p.Key.Rating12v12);
            // Formula is the difference between loosing and winning team of avg rating divided by 40
            var scoreDelta = CalculateScoreDelta(averageWinningRating, averageLosingRating);

            return GetPlayersChange12v12(teams, BaseRatingChange12V12 + scoreDelta);
        }

        private Dictionary<Player, GameResult> SplitTeams(IEnumerable<MatchPlayer> matchPlayers, ushort winnerTeam)
        {
            var result = new Dictionary<Player, GameResult>();
            foreach (var matchPlayer in matchPlayers)
                result.Add(matchPlayer.Player, matchPlayer.Team == winnerTeam ? GameResult.Winner : GameResult.Loser);
            return result;
        }

        private Dictionary<ulong, PlayerRatingChange> GetPlayersChange12v12(Dictionary<Player, GameResult> teams, int scoreDelta)
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

        #endregion

        #region Overthrow

        private Dictionary<string, List<int>> _scores = new Dictionary<string, List<int>>()
        {
            { "forest_solo", new List<int> { 30, 25, 18, 11, 4, -4, -11, -18, -25, -30 } },
            { "core_quartet", new List<int> { 30, 18, 6, -6, -18, -30 } },
            { "desert_duo", new List<int> { 30, 15, 0, -15, -30 } },
            { "temple_quartet", new List<int> { 30, 10, -10, -30 } },
            { "mines_trio", new List<int> { 30, 0, -30 } },
            { "desert_quintet", new List<int> { 30, 0, -30 } },
            { "desert_octet", new List<int> { 30, 0, -30 } }
        };
        public Dictionary<ulong, PlayerRatingChange> RecordRankedMatchOverwatch(IEnumerable<MatchPlayer> matchPlayers, string mapName)
        {
            var result = new Dictionary<ulong, PlayerRatingChange>();

            var teams = matchPlayers
                .GroupBy(t => t.Team)
                .OrderByDescending(g => g.Sum(p => p.Kills))
                .ThenBy(g => g.Max(p => p.LastKill))
                .ToDictionary(g => g.Key, g => g.Select(mp => mp.Player).ToList());
            var teamScores = teams.Keys
                .Zip(_scores[mapName], (k, v) => new { k, v })
                .ToDictionary(x => x.k, x => x.v);

            foreach (var team in teams)
            {
                var averageCurrentTeamRating = team.Value
                    .Average(x => x.PlayerOverthrowRating.FirstOrDefault(p => p.MapName == mapName)?.Rating);
                var averageOtherTeamRating = teams
                    .Where(x => x.Key != team.Key)
                    .SelectMany(t => t.Value)
                    .Average(x => x.PlayerOverthrowRating.FirstOrDefault(p => p.MapName == mapName)?.Rating);

                var scoreDelta = CalculateScoreDelta(averageCurrentTeamRating ?? PlayerOverthrowRating.DefaultRating,
                                          averageOtherTeamRating ?? PlayerOverthrowRating.DefaultRating);
                var ratingChange = teamScores[team.Key] > 0  ? teamScores[team.Key] + scoreDelta : teamScores[team.Key] - scoreDelta;

                GetPlayersChangeOverthrow(team.Value, ratingChange, result, mapName);
            }

            return result;
        }

        private void GetPlayersChangeOverthrow(List<Player> players, int scoreChange, Dictionary<ulong, PlayerRatingChange> result, string mapName)
        {
            foreach (var player in players)
            {
                var playerChange = new PlayerRatingChange() { Old = player.PlayerOverthrowRating.FirstOrDefault(x => x.MapName == mapName).Rating };
                player.PlayerOverthrowRating.FirstOrDefault(x => x.MapName == mapName).Rating += scoreChange;
                playerChange.New = playerChange.Old + scoreChange;
                result.Add(player.SteamId, playerChange);
            }
        }

        #endregion


        private const int MaximumDeltaRatingChange = 25;
        private int CalculateScoreDelta(double averageWinningRating, double averageLosingRating)
        {
            // e.g. -(1900 - 2100) / 40 = 5. Meaning winning team will get 5 more points
            // than the base as their average was weaker
            var scoreDeltaDouble = -(averageWinningRating - averageLosingRating) / 40;
            var scoreDelta = (int)(Math.Round(scoreDeltaDouble, 0, MidpointRounding.AwayFromZero));
            return Math.Min(scoreDelta, MaximumDeltaRatingChange);
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
