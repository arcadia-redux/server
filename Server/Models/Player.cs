using Server.Enums;
using Server.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

#nullable enable
namespace Server.Models
{
    public class Player
    {      
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
        [NotMapped]
        public const int DefaultRating = 2000;
    }

    public class PlayerOverthrowRating
    {
        [NotMapped]
        public const int DefaultRating = 2000;
        public string MapName { get; set; }
        public int Rating { get; set; }

        public static IEnumerable<PlayerOverthrowRating> GetDefaultRatings()
        {
            return Enum.GetValues(typeof(MapEnum)).Cast<MapEnum>()
                .Select(e => new PlayerOverthrowRating()
                {
                    MapName = e.GetDescription(),
                    Rating = DefaultRating
                });
        }
    }
}
