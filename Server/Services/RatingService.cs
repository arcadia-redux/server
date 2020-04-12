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

        public async Task<List<LeaderBoardPlayer>> GetTopPlayers()
        {
            return await _context.Players
                            .OrderByDescending(p => p.Rating12v12)
                            .Take(NumberOfTopPlayers)
                            .Select(p => new LeaderBoardPlayer { SteamId = p.SteamId, Rating = p.Rating12v12 })
                            .OrderByDescending(p => p.Rating)
                            .ToListAsync();
        }

        public async Task UpdateNewRating(IEnumerable<Player> winners, IEnumerable<Player> losers)
        {
            var averageWinningRating = winners.Average(p => p.Rating12v12);
            var averageLosingRating = losers.Average(p => p.Rating12v12);
            // Formula is the difference between loosing and winning team of avg rating divided by 40
            var scoreDelta = CalculateScoreDelta(averageWinningRating, averageLosingRating);
            await UpdateRating(winners, losers, scoreDelta);
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

        private async Task UpdateRating(IEnumerable<Player> winningTeam, IEnumerable<Player> losingTeam, int scoreDelta)
        {
            foreach (var player in winningTeam)
                player.Rating12v12 += BaseRating + scoreDelta;
            foreach (var player in losingTeam)
                player.Rating12v12 -= BaseRating + scoreDelta;
            _context.UpdateRange(winningTeam);
            _context.UpdateRange(losingTeam);
        }

        public Dictionary<GameResult, List<Player>> SplitTeams(Dictionary<string, ushort> playersKvp, ushort winnerTeam, List<Player> players)
        {
            var teamsSplit = new Dictionary<GameResult, List<Player>>()
            {
                { GameResult.Winner, new List<Player>() },
                { GameResult.Loser, new List<Player>() }
            };
            foreach (var player in players)
            {
                var playerTeam = playersKvp[player.SteamId.ToString()];
                if (playerTeam == winnerTeam)
                    teamsSplit[GameResult.Winner].Add(player);
                else
                    teamsSplit[GameResult.Loser].Add(player);
            }

            return teamsSplit;
        }
    }

    public enum GameResult
    {
        Winner,
        Loser
    }

    public class LeaderBoardPlayer
    {
        public ulong SteamId { get; set; }
        public int Rating { get; set; }
    }
}
