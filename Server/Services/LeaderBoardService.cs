using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Server.Controllers;
using Server.Models;

namespace Server.Services
{
    public class LeaderBoardService
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;
        private readonly int _numberOfTopPlayers;
        private readonly int _maximumDelta;
        private readonly int _baseRating;
        private readonly int _divisionPoints;

        public LeaderBoardService(IConfiguration configuration, AppDbContext context)
        {
            _configuration = configuration;
            _context = context;
            _numberOfTopPlayers = Int32.Parse(configuration["NumberTopPlayers"]);
            _maximumDelta = Int32.Parse(configuration["MaximumDelta"]);
            _baseRating = Int32.Parse(configuration["BaseRating"]);
            _divisionPoints = Int32.Parse(configuration["DivisionPoints"]);
        }

        /// <summary>
        /// Should return the top 100 top players. If you need to change that, change the constant at the beginning
        /// </summary>
        /// <returns></returns>
        public async Task<List<LeaderBoardPlayer>> GetTopPlayers()
        {
            return await _context.Players
                            .OrderByDescending(p => p.Rating12v12)
                            .Take(_numberOfTopPlayers)
                            .Select(p => new LeaderBoardPlayer { SteamId = p.SteamId, Rating = p.Rating12v12 })
                            .ToListAsync();
        }

        /// <summary>
        /// It gets winning teams, losing teams and then updates the new rating
        /// </summary>
        /// <param name="request"></param>
        /// <param name="players"></param>
        /// <returns></returns>
        public async Task<List<Player>> UpdateNewRating(AfterMatchRequest request, List<Player> players)
        {
            var winningTeam = GetWinningTeam(request, players, out var averageWinningRating);
            var losingTeam = GetLosingTeam(request, players, out var averageLosingRating);
            var scoreDelta = CalculateScoreDelta(averageWinningRating, averageLosingRating);
            await UpdateRating(winningTeam, losingTeam, scoreDelta);
            return players;
        }


        /// <summary>
        /// Formula is the difference between loosing and winning team of avg rating divided by 40
        /// </summary>
        /// <param name="averageWinningRating"></param>
        /// <param name="averageLosingRating"></param>
        /// <returns></returns>
        private int CalculateScoreDelta(double averageWinningRating, double averageLosingRating)
        {
            var scoreDeltaDouble =
                -(averageWinningRating - averageLosingRating) /
                _divisionPoints; // e.g. - (1900 - 2100) / 40 = 5. Meaning winning team will get 5 more points 
                                // than the base as their average was weaker
            var scoreDelta =
                Convert.ToInt32(Math.Round(scoreDeltaDouble, 0,
                    MidpointRounding.AwayFromZero)); // we don't do a conversion straight to the double
                                                     // as it will do a MidpointRounding.ToEven by default
            scoreDelta = scoreDelta > _maximumDelta ? _maximumDelta : scoreDelta;
            return scoreDelta;
        }

        private IEnumerable<Player> GetLosingTeam(AfterMatchRequest request, List<Player> allPlayers, out double averageLosingRating)
        {
            var losingTeamIds = request
                .Players
                .Where(p => p.Team != request.Winner)
                .Select(p => ulong.Parse(p.SteamId))
                .ToList();
            var losingTeam = allPlayers.Where(p => losingTeamIds.Contains(p.SteamId));
            averageLosingRating = losingTeam.Average(p => p.Rating12v12);
            return losingTeam;
        }

        private IEnumerable<Player> GetWinningTeam(AfterMatchRequest request, List<Player> allPlayers, out double averageWinningRating)
        {
            var winningTeamIds = request
                .Players
                .Where(p => p.Team == request.Winner)
                .Select(p => ulong.Parse(p.SteamId))
                .ToList();
            var winningTeam = allPlayers.Where(p => winningTeamIds.Contains(p.SteamId));
            averageWinningRating = winningTeam.Average(p => p.Rating12v12);
            return winningTeam;
        }

        private async Task UpdateRating(IEnumerable<Player> winningTeam, IEnumerable<Player> losingTeam, int scoreDelta)
        {
            foreach (var player in winningTeam)
                player.Rating12v12 += _baseRating + scoreDelta;
            foreach (var player in losingTeam)
                player.Rating12v12 -= _baseRating + scoreDelta;
            _context.UpdateRange(winningTeam);
            _context.UpdateRange(losingTeam);
        }
    }
}
