using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Server.Models
{
    public class Player
    {
        [Key] public ulong SteamId { get; set; }

        public IEnumerable<MatchPlayer> Matches { get; set; }
        public string Comment { get; set; }
        public ushort PatreonLevel { get; set; }
        public bool? PatreonEmblemEnabled { get; set; }
        public string PatreonEmblemColor { get; set; }
        public bool? PatreonBootsEnabled { get; set; }
    }
}
