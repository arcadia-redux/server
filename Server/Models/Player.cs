using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable enable
namespace Server.Models
{
    public class Player
    {
        [NotMapped]
        public const int DefaultRating = 2000;

        [Key] public ulong SteamId { get; set; }
        public string? Comment { get; set; }
        public IEnumerable<MatchPlayer> Matches { get; set; } = null!;
        public ushort PatreonLevel { get; set; }
        public DateTime? PatreonEndDate { get; set; }

        public bool? PatreonEmblemEnabled { get; set; }
        public string? PatreonEmblemColor { get; set; }
        public bool? PatreonBootsEnabled { get; set; }
        public List<int>? PatreonChatWheelFavorites { get; set; }
        [Column(TypeName = "jsonb")]
        public Dictionary<string, object>? PatreonCosmetics { get; set; }
        public int Rating12v12 { get; set; }
        public IEnumerable<PlayerOverthrowRating> PlayerOverthrowRating { get; set; }
    }

    public class PlayerOverthrowRating
    {
        [NotMapped]
        public const int DefaultRating = 2000;
        public string MapName { get; set; }
        public int Rating { get; set; }
    }
    //public class PlayerOverthrow
    //{
    //    [NotMapped]
    //    public const int DefaultRating = 2000;
    //    public int MinesTrio { get; set; }
    //    public int DesertDuo { get; set; }
    //    public int ForestSolo { get; set; }
    //    public int DesertQuintet { get; set; }
    //    public int TempleQuartet { get; set; }
    //    public int DesertOctet { get; set; }
    //    public int TempleSextet { get; set; }
    //    public int CoreQuartet { get; set; }
    //}
}
