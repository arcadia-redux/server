using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Server.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CustomGame
    {
        Overthrow,
        Dota12v12,
    }

    public class Match
    {
        public long MatchId { get; set; }
        public string MapName { get; set; }
        public CustomGame CustomGame { get; set; }
        public ushort Winner { get; set; }
        public uint Duration { get; set; }
        public DateTime EndedAt { get; set; }

        public IEnumerable<MatchPlayer> Players { get; set; }
    }

    public class MatchPlayer
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long MatchId { get; set; }

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
        [NotMapped]
        public DateTime LastKill { get; set; }
    }
}
