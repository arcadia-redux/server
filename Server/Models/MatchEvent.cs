using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models
{
    public class MatchEvent
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public long MatchId { get; set; }
        public object Body { get; set; }
    }

    public class PaymentUpdateMatchEventBody
    {
        public string Kind { get => "paymentUpdate"; }
        public string SteamId { get; set; }
        public string PayerSteamId { get; set; }
        public string Error { get; set; }
        public ushort? Level { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
