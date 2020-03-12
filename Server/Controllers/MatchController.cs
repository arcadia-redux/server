using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Server.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class MatchController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger _logger;

        public MatchController(AppDbContext context, ILogger<MatchController> logger)
        {
            _context = context;
            _logger = logger;
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
                        .GroupBy(m => m.Hero)
                        .OrderByDescending(g => g.Count())
                        .Take(10)
                        .Select(g => g.Key)
                        .ToList(),
                    HeroesGlobal = p.Matches
                        .OrderByDescending(m => m.MatchId)
                        .Take(100)
                        .GroupBy(m => m.Hero)
                        .OrderByDescending(g => g.Count())
                        .Take(10)
                        .Select(g => g.Key)
                        .ToList(),
                })
                .ToListAsync();

            return new AutoPickResponse()
            {
                Players = players.Select(p => new AutoPickResponse.Player()
                {
                    SteamId = p.SteamId.ToString(),
                    Heroes = (p.HeroesMap.Except(selectedHeroes).Count() >= 3 ? p.HeroesMap : p.HeroesGlobal)
                        .Except(selectedHeroes)
                        .Take(3)
                        .ToList(),
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
                    Patreon =
                        new BeforeMatchResponse.Patreon()
                        {
                            EndDate = p.PatreonEndDate,
                            Level = p.PatreonLevel,
                            EmblemEnabled = p.PatreonEmblemEnabled ?? true,
                            EmblemColor = p.PatreonEmblemColor ?? "White",
                            BootsEnabled = p.PatreonBootsEnabled ?? true,
                            ChatWheelFavorites = p.PatreonChatWheelFavorites ?? new List<int>(),
                        },
                    MatchesOnMap = p.Matches
                        .Where(m => m.Match.CustomGame == customGame)
                        .Where(m => m.Match.MapName == mapName)
                        .OrderByDescending(m => m.MatchId)
                        .Select(m => new { IsWinner = m.Team == m.Match.Winner, m.Kills, m.Deaths, m.Assists })
                        .ToList(),
                    SmartRandomHeroesMap = p.Matches
                        .Where(m => m.Match.CustomGame == customGame && m.Match.MapName == mapName && m.PickReason == "pick")
                        .OrderByDescending(m => m.MatchId)
                        .Take(100)

                        .GroupBy(m => m.Hero)
                        .Where(g => g.Count() >= (int)Math.Ceiling(Math.Min(p.Matches.Count(m => m.Match.CustomGame == customGame && m.Match.MapName == mapName && m.PickReason == "pick"), 100) / 20.0))
                        .Select(g => g.Key)

                        .ToList(),
                    SmartRandomHeroesGlobal = p.Matches
                        .Where(m => m.Match.CustomGame == customGame && m.PickReason == "pick")
                        .OrderByDescending(m => m.MatchId)
                        .Take(100)

                        .GroupBy(m => m.Hero)
                        .Where(g => g.Count() >= (int)Math.Ceiling(Math.Min(p.Matches.Count(m => m.Match.CustomGame == customGame && m.PickReason == "pick"), 100) / 20.0))
                        .Select(g => g.Key)

                        .ToList(),
                    LastSmartRandomUse = p.Matches
                        .Where(m => m.Match.CustomGame == customGame)
                        .Where(m => m.PickReason == "smart-random")
                        .OrderByDescending(m => m.Match.EndedAt)
                        .Take(1)
                        .Select(m => m.Match.EndedAt)
                        .FirstOrDefault()
                })
                .ToListAsync();

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

                        var player = new BeforeMatchResponse.Player
                        {
                            SteamId = id.ToString(),
                            Patreon = response.Patreon,
                            Streak = response.MatchesOnMap.TakeWhile(w => w.IsWinner).Count(),
                            BestStreak = response.MatchesOnMap.LongestStreak(w => w.IsWinner),
                            AverageKills = response.MatchesOnMap
                                .Select(x => (double)x.Kills)
                                .DefaultIfEmpty()
                                .Average(),
                            AverageDeaths = response.MatchesOnMap
                                .Select(x => (double)x.Deaths)
                                .DefaultIfEmpty()
                                .Average(),
                            AverageAssists = response.MatchesOnMap
                                .Select(x => (double)x.Assists)
                                .DefaultIfEmpty()
                                .Average(),
                            Wins = response.MatchesOnMap.Count(w => w.IsWinner),
                            Loses = response.MatchesOnMap.Count(w => !w.IsWinner),
                        };

                        var heroes = response.SmartRandomHeroesMap.Count >= 5
                            ? response.SmartRandomHeroesMap
                            : response.SmartRandomHeroesGlobal;

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
                    .ToList()
            };
        }

        [HttpPost]
        [Route("after")]
        public async Task<ActionResult> After([FromBody] AfterMatchRequest request)
        {
            var requestedSteamIds = request.Players.Select(p => ulong.Parse(p.SteamId)).ToList();

            var existingPlayers = await _context.Players
                .Where(p => requestedSteamIds.Contains(p.SteamId))
                .ToListAsync();

            var newPlayers = request.Players
                .Where(r => existingPlayers.All(p => p.SteamId.ToString() != r.SteamId))
                .Select(p => new Player() { SteamId = ulong.Parse(p.SteamId) })
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
            await _context.SaveChangesAsync();

            return Ok();
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
        }
    }

    public class MatchEventsRequest
    {
        [Required] public long MatchId { get; set; }
    }
}
