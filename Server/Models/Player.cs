using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

#nullable enable
namespace Server.Models
{
    public class Player
    {
        public const int DefaultRating = 2000;

        [Key] public ulong SteamId { get; set; }
        public IEnumerable<MatchPlayer> Matches { get; set; } = null!;
        public string? Comment { get; set; }
        public ushort PatreonLevel { get; set; }
        public DateTime? PatreonEndDate { get; set; }

        public bool? PatreonEmblemEnabled { get; set; }
        public string? PatreonEmblemColor { get; set; }
        public bool? PatreonBootsEnabled { get; set; }
        public List<int>? PatreonChatWheelFavorites { get; set; }
        [Column(TypeName = "jsonb")]
        public Dictionary<string, object>? PatreonCosmetics { get; set; }
        public int Rating12v12 { get; set; }
    }

    public static class PlayerExtensions
    {
        public static async Task<Player> FindOrCreatePlayer(this DbSet<Player> players, ulong steamId) =>
            await players.FindAsync(steamId) ?? players.Add(new Player { SteamId = steamId }).Entity;
    }
}
