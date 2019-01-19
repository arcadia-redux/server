using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JetBrains.Annotations;

namespace Server.Models
{
    public class Match
    {
        public uint MatchId { get; set; }
        public string MapName { get; set; }
        public ushort Winner { get; set; }
        public uint Duration { get; set; }
        public DateTime EndedAt { get; set; }

        public IEnumerable<MatchPlayer> Players { get; set; }
    }

    public class MatchPlayer
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public uint MatchId { get; set; }

        [ForeignKey("MatchId")] public Match Match { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public ulong SteamId { get; set; }

        [ForeignKey("SteamId")] public Player Player { get; set; }

        public ushort PlayerId { get; set; }
        public ushort Team { get; set; }
        public string Hero { get; set; }
        public string PickReason { get; set; }
        public uint Kills { get; set; }
        public uint Deaths { get; set; }
        public uint Assists { get; set; }
        public uint Level { get; set; }
        public List<MatchPlayerItem> Items { get; set; }
    }

    [UsedImplicitly]
    public class MatchPlayerItem
    {
        // TODO: Use composite key
        public long Id { get; set; }
        [Required] public ushort Slot { get; set; }
        [Required] public string Name { get; set; }
        [Required] public uint Charges { get; set; }
    }
}
