using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Models;
using Server.Services;

namespace Server.Controllers
{
    [Authorize("Lua")]
    [ApiController]
    [Route("api/[controller]")]
    public class MatchController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly RatingService _ratingService;

        public MatchController(AppDbContext context, RatingService ratingService)
        {
            _context = context;
            _ratingService = ratingService;
        }

        [HttpPost]
        [Route("auto-pick")]
        public async Task<ActionResult<AutoPickResponse>> AutoPick(AutoPickRequest request)
        {
            var requestedSteamIds = request.Players.Select(ulong.Parse).ToList();
            var players = await _context.Players
                .Where(p => requestedSteamIds.Contains(p.SteamId))
                .Select(p => new
                {
                    p.SteamId,
                    HeroesOnMap = p.Matches
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
                .Except(request.SelectedHeroes)
                .GroupBy(x => x)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => g.Key)
                .ToList();

            return new AutoPickResponse()
            {
                Players = players.Select(p =>
                {
                    var heroesOnMap = GetBestHeroes(p.HeroesOnMap);
                    var heroesGlobal = GetBestHeroes(p.HeroesGlobal);
                    var heroes = (heroesOnMap.Count() >= 3 ? heroesOnMap : heroesGlobal)
                        .Take(3)
                        .ToList();

                    return new AutoPickResponse.Player() { SteamId = p.SteamId.ToString(), Heroes = heroes };
                })
            };
        }

        [HttpPost]
        [Route("before")]
        public async Task<BeforeMatchResponse> Before(BeforeMatchRequest request)
        {
            var mapName = request.MapName;

            var requestedSteamIds = request.Players.Select(ulong.Parse).ToList();
            var responses = await _context.Players
                .Where(p => requestedSteamIds.Contains(p.SteamId))
                .Select(p => new
                {
                    p.SteamId,
                    p.Rating12v12,
                    Patreon = new BeforeMatchResponse.Patreon()
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
                        .Where(m => m.Match.CustomGame == request.CustomGame)
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
                .AsNoTracking()
                .ToListAsync();

            return new BeforeMatchResponse()
            {
                Players = requestedSteamIds
                    .Select(steamId =>
                    {
                        var response = responses.FirstOrDefault(p => p.SteamId == steamId);
                        if (response == null)
                        {
                            return new BeforeMatchResponse.Player()
                            {
                                SteamId = steamId.ToString(),
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
                            response.Patreon.Level = 0;

                        var matchesOnMap = response.Matches.Where(m => m.MapName == mapName).ToList();

                        var player = new BeforeMatchResponse.Player
                        {
                            SteamId = steamId.ToString(),
                            Patreon = response.Patreon,
                            Streak = matchesOnMap.TakeWhile(w => w.IsWinner).Count(),
                            BestStreak = matchesOnMap.LongestStreak(w => w.IsWinner),
                            AverageKills = matchesOnMap.Select(x => (double)x.Kills).DefaultIfEmpty().Average(),
                            AverageDeaths = matchesOnMap.Select(x => (double)x.Deaths).DefaultIfEmpty().Average(),
                            AverageAssists = matchesOnMap.Select(x => (double)x.Assists).DefaultIfEmpty().Average(),
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
                            player.SmartRandomHeroes = heroes;
                        else
                            player.SmartRandomHeroesError = "no_stats";

                        return player;
                    })
                    .ToList(),

                Leaderboard = request.CustomGame == CustomGame.Dota12v12 ? await _ratingService.GetLeaderboard() : null,
                MapPlayersRating = await _ratingService.GetMapPlayersRating(requestedSteamIds)
            };
        }

        [HttpPost]
        [Route("after")]
        public async Task<AfterMatchResponse> After([FromBody] AfterMatchRequest request)
        {
            var requestedSteamIds = request.Players.Select(p => ulong.Parse(p.SteamId)).ToList();
            var existingPlayers = await _context.Players.Where(p => requestedSteamIds.Contains(p.SteamId)).ToListAsync();
            var newPlayers = requestedSteamIds
                .Where(id => existingPlayers.All(p => p.SteamId != id))
                .Select(id => new Player() { SteamId = id, Rating12v12 = Player.DefaultRating })
                .ToList();

            var allPlayers = existingPlayers.Concat(newPlayers);

            foreach (var playerUpdate in request.Players)
            {
                if (playerUpdate.PatreonUpdate == null) continue;
                var player = allPlayers.Single(p => p.SteamId.ToString() == playerUpdate.SteamId);
                player.PatreonBootsEnabled = playerUpdate.PatreonUpdate.BootsEnabled;
                player.PatreonEmblemEnabled = playerUpdate.PatreonUpdate.EmblemEnabled;
                player.PatreonEmblemColor = playerUpdate.PatreonUpdate.EmblemColor;
                player.PatreonChatWheelFavorites = playerUpdate.PatreonUpdate.ChatWheelFavorites;
                player.PatreonCosmetics = playerUpdate.PatreonUpdate.Cosmetics;
            }

            var match = new Match
            {
                CustomGame = request.CustomGame,
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
        [Required] public CustomGame CustomGame { get; set; }
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
        [Required] public CustomGame CustomGame { get; set; }
        [Required] public string MapName { get; set; }
        [Required] public List<string> Players { get; set; }
    }

    public class BeforeMatchResponse
    {
        public IEnumerable<Player> Players { get; set; }
        public IEnumerable<LeaderboardPlayer> Leaderboard { get; set; }

        public IEnumerable<LeaderboardPlayer> MapPlayersRating { get; set; }

        public class Player
        {
            public string SteamId { get; set; }
            public List<string>? SmartRandomHeroes { get; set; }
            public string? SmartRandomHeroesError { get; set; }
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
