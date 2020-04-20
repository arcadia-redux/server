using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Server.Models;
using Server.Services;

namespace Server.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class MatchController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger _logger;
        private readonly RatingService _ratingService;

        public MatchController(AppDbContext context, ILogger<MatchController> logger, RatingService ratingService)
        {
            _context = context;
            _logger = logger;
            _ratingService = ratingService;
        }

        [HttpPost]
        [Route("auto-pick")]
        public async Task<ActionResult<AutoPickResponse>> AutoPick(AutoPickRequest request)
        {
            var realSteamIds = request.Players.Select(ulong.Parse).ToList();
            var selectedHeroes = request.SelectedHeroes;
            var players = await _context.Players
                .Where(p => realSteamIds.Contains(p.SteamId))
                .Select(p => new
                {
                    SteamId = p.SteamId.ToString(),
                    HeroesMap = p.Matches
                        .Where(m => m.Match.MapName == request.MapName)
                        .OrderByDescending(m => m.MatchId)
                        .Take(100)
                        .Select(m => m.Hero)
                        .ToList(),
                    HeroesGlobal = p.Matches
                        .OrderByDescending(m => m.MatchId)
                        .Take(100)
                        .Select(m => m.Hero)
                        .ToList(),
                })
                .ToListAsync();

            List<string> GetBestHeroes(List<string> heroes) => heroes
                .Except(selectedHeroes)
                .GroupBy(x => x)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => g.Key)
                .ToList();

            return new AutoPickResponse()
            {
                Players = players.Select(p =>
                {
                    var bestHeroesOnMap = GetBestHeroes(p.HeroesMap);
                    var bestHeroesGlobal = GetBestHeroes(p.HeroesGlobal);

                    return new AutoPickResponse.Player()
                    {
                        SteamId = p.SteamId.ToString(),
                        Heroes = (bestHeroesOnMap.Count() >= 3 ? bestHeroesOnMap : bestHeroesGlobal)
                            .Take(3)
                            .ToList(),
                    };
                })
            };
        }

        [HttpPost]
        [Route("before")]
        public async Task<BeforeMatchResponse> Before(BeforeMatchRequest request)
        {
            var customGame = request.CustomGame.Value;
            var mapName = request.MapName;

            var realSteamIds = request.Players.Select(ulong.Parse).ToList();
            var responses = await _context.Players
                .Where(p => realSteamIds.Contains(p.SteamId))
                .Select(p => new
                {
                    SteamId = p.SteamId.ToString(),
                    p.Rating12v12,
                    Patreon =
                        new BeforeMatchResponse.Patreon()
                        {
                            EndDate = p.PatreonEndDate,
                            Level = p.PatreonLevel,
                            EmblemEnabled = p.PatreonEmblemEnabled ?? true,
                            EmblemColor = p.PatreonEmblemColor ?? "White",
                            BootsEnabled = p.PatreonBootsEnabled ?? true,
                            ChatWheelFavorites = p.PatreonChatWheelFavorites ?? new List<int>(),
                            Cosmetics = p.PatreonCosmetics,
                        },
                    Matches = p.Matches
                        .Where(m => m.Match.CustomGame == customGame)
                        .OrderByDescending(m => m.MatchId)
                        .Select(mp => new
                        {
                            mp.Kills,
                            mp.Deaths,
                            mp.Assists,
                            mp.Match.MapName,
                            mp.PickReason,
                            mp.Hero,
                            IsWinner = mp.Team == mp.Match.Winner,
                        })
                        .ToList()
                })
                .ToListAsync();
            var playersTop = await _ratingService.GetLeaderboard(customGame, mapName);
            return new BeforeMatchResponse()
            {
                Players = request.Players
                    .Select(id =>
                    {
                        var response = responses.FirstOrDefault(p => p.SteamId == id);
                        if (response == null)
                        {
                            return new BeforeMatchResponse.Player()
                            {
                                SteamId = id.ToString(),
                                Patreon = new BeforeMatchResponse.Patreon()
                                {
                                    Level = 0,
                                    EmblemEnabled = true,
                                    EmblemColor = "White",
                                    BootsEnabled = true,
                                    ChatWheelFavorites = new List<int>(),
                                },
                                SmartRandomHeroesError = "no_stats",
                            };
                        }

                        if (response.Patreon.EndDate < DateTime.UtcNow)
                        {
                            response.Patreon.Level = 0;
                        }

                        var matchesOnMap = response.Matches.Where(m => m.MapName == mapName).ToList();

                        var player = new BeforeMatchResponse.Player
                        {
                            SteamId = id.ToString(),
                            Patreon = response.Patreon,
                            Streak = matchesOnMap.TakeWhile(w => w.IsWinner).Count(),
                            BestStreak = matchesOnMap.LongestStreak(w => w.IsWinner),
                            AverageKills = matchesOnMap
                                .Select(x => (double)x.Kills)
                                .DefaultIfEmpty()
                                .Average(),
                            AverageDeaths = matchesOnMap
                                .Select(x => (double)x.Deaths)
                                .DefaultIfEmpty()
                                .Average(),
                            AverageAssists = matchesOnMap
                                .Select(x => (double)x.Assists)
                                .DefaultIfEmpty()
                                .Average(),
                            Wins = matchesOnMap.Count(w => w.IsWinner),
                            Loses = matchesOnMap.Count(w => !w.IsWinner),
                        };

                        List<string> GetSmartRandomHeroes(bool onMap)
                        {
                            var matches = (onMap ? matchesOnMap : response.Matches).Where(m => m.PickReason == "pick");
                            return matches
                                .Take(100)
                                .GroupBy(m => m.Hero)
                                .Where(g => g.Count() >= (int)Math.Ceiling(Math.Min(matches.Count(), 100) / 20.0))
                                .Select(g => g.Key)
                                .ToList();
                        };

                        var smartRandomHeroesMap = GetSmartRandomHeroes(true);
                        var smartRandomHeroesGlobal = GetSmartRandomHeroes(false);

                        var heroes = smartRandomHeroesMap.Count >= 5
                            ? smartRandomHeroesMap
                            : smartRandomHeroesGlobal;

                        if (heroes.Count >= 3)
                        {
                            player.SmartRandomHeroes = heroes;
                        }
                        else
                        {
                            player.SmartRandomHeroesError = "no_stats";
                        }

                        return player;
                    })
                    .ToList(),
                Leaderboard = playersTop,
            };
        }

        [HttpPost]
        [Route("after")]
        public async Task<AfterMatchResponse> After([FromBody] AfterMatchRequest request)
        {
            var requestedSteamIds = request.Players.Select(p => ulong.Parse(p.SteamId)).ToList();

            var existingPlayers = await _context.Players
                .Where(p => requestedSteamIds.Contains(p.SteamId))
                .ToListAsync();

            var newPlayers = request.Players
                .Where(r => existingPlayers.All(p => p.SteamId.ToString() != r.SteamId))
                .Select(p => new Player() { SteamId = ulong.Parse(p.SteamId), Rating12v12 = Player.DefaultRating })
                .ToList();

            foreach (var playerUpdate in request.Players.Where(p => p.PatreonUpdate != null))
            {
                var player =
                    existingPlayers.FirstOrDefault(p => p.SteamId.ToString() == playerUpdate.SteamId) ??
                    newPlayers.FirstOrDefault(p => p.SteamId.ToString() == playerUpdate.SteamId);
                // TODO: Shouldn't be the case ever?
                if (player == null) continue;

                player.PatreonBootsEnabled = playerUpdate.PatreonUpdate.BootsEnabled;
                player.PatreonEmblemEnabled = playerUpdate.PatreonUpdate.EmblemEnabled;
                player.PatreonEmblemColor = playerUpdate.PatreonUpdate.EmblemColor;
                player.PatreonChatWheelFavorites = playerUpdate.PatreonUpdate.ChatWheelFavorites;
                player.PatreonCosmetics = playerUpdate.PatreonUpdate.Cosmetics;
            }

            var match = new Match
            {
                CustomGame = request.CustomGame.Value,
                MatchId = request.MatchId,
                MapName = request.MapName,
                Winner = request.Winner,
                Duration = request.Duration,
                EndedAt = DateTime.UtcNow
            };

            match.Players = request.Players
                .Select(p => new MatchPlayer
                {
                    Match = match,
                    SteamId = ulong.Parse(p.SteamId),
                    PlayerId = p.PlayerId,
                    Team = p.Team,
                    Hero = p.Hero,
                    PickReason = p.PickReason,
                    Kills = p.Kills,
                    Deaths = p.Deaths,
                    Assists = p.Assists,
                    Level = p.Level,
                })
                .ToList();
            _context.AddRange(newPlayers);
            _context.Matches.Add(match);

            var ratingChanges = request.CustomGame == CustomGame.Dota12v12 ? _ratingService.RecordRankedMatch(match.Players, request.Winner) : null;

            await _context.SaveChangesAsync();

            return new AfterMatchResponse()
            {
                Players = match.Players
                    .Select(p => new AfterMatchResponse.Player()
                    {
                        SteamId = p.SteamId.ToString(),
                        RatingChange = ratingChanges?[p.SteamId],
                    })
                    .ToList(),
            };
        }

        [HttpPost]
        [Route("events")]
        public async Task<List<object>> Events([FromBody] MatchEventsRequest request)
        {
            var matchId = request.MatchId;
            var events = await _context.MatchEvents.Where(e => e.MatchId == matchId).ToListAsync();

            _context.MatchEvents.RemoveRange(events);
            await _context.SaveChangesAsync();

            return events.Select(e => e.Body).ToList();
        }
    }

    public class AfterMatchRequest
    {
        [Required] public CustomGame? CustomGame { get; set; }
        [Required] public long MatchId { get; set; }
        [Required] public string MapName { get; set; }
        [Required] public ushort Winner { get; set; }
        [Required] public uint Duration { get; set; }

        [Required] public IEnumerable<Player> Players { get; set; }

        public class Player
        {
            [Required] public ushort PlayerId { get; set; }
            [Required] public string SteamId { get; set; }
            [Required] public ushort Team { get; set; }
            [Required] public string Hero { get; set; }
            [Required] public string PickReason { get; set; }
            [Required] public uint Kills { get; set; }
            [Required] public uint Deaths { get; set; }
            [Required] public uint Assists { get; set; }
            [Required] public uint Level { get; set; }
            // TODO: We don't store it anymore
            [Required] public List<object> Items { get; set; }
            public PatreonUpdate PatreonUpdate { get; set; }
        }

        public class PatreonUpdate
        {
            public bool EmblemEnabled { get; set; }
            public string EmblemColor { get; set; }
            public bool BootsEnabled { get; set; }
            // TODO: Required?
            public List<int>? ChatWheelFavorites { get; set; }
            public Dictionary<string, object>? Cosmetics { get; set; }
        }
    }

    public class AfterMatchResponse
    {
        public IEnumerable<Player> Players { get; set; }

        public class Player
        {
            public string SteamId { get; set; }
            public PlayerRatingChange RatingChange { get; set; }
        }
    }

    public class AutoPickRequest
    {
        [Required] public string MapName { get; set; }
        [Required] public List<string> SelectedHeroes { get; set; }
        [Required] public List<string> Players { get; set; }
    }

    public class AutoPickResponse
    {
        public IEnumerable<Player> Players { get; set; }

        public class Player
        {
            public string SteamId { get; set; }
            public List<string> Heroes { get; set; }
        }
    }

    public class BeforeMatchRequest
    {
        [Required] public CustomGame? CustomGame { get; set; }
        [Required] public string MapName { get; set; }
        [Required] public List<string> Players { get; set; }
    }

    public class BeforeMatchResponse
    {
        public IEnumerable<Player> Players { get; set; }
        public IEnumerable<LeaderboardPlayer> Leaderboard { get; set; }

        public class Player
        {
            public string SteamId { get; set; }
            public List<string> SmartRandomHeroes { get; set; }
            public string SmartRandomHeroesError { get; set; }
            public int Streak { get; set; }
            public int BestStreak { get; set; }
            public double AverageKills { get; set; }
            public double AverageDeaths { get; set; }
            public double AverageAssists { get; set; }
            public int Wins { get; set; }
            public int Loses { get; set; }
            public Patreon Patreon { get; set; }
        }

        public class Patreon
        {
            public DateTime? EndDate { get; set; }
            public ushort Level { get; set; }
            public bool EmblemEnabled { get; set; }
            public string EmblemColor { get; set; }
            public bool BootsEnabled { get; set; }
            public List<int> ChatWheelFavorites { get; set; }
            public Dictionary<string, object>? Cosmetics { get; set; }
        }
    }

    public class MatchEventsRequest
    {
        [Required] public long MatchId { get; set; }
    }
}
