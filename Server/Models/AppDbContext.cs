using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Server.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
        }
        public DbSet<Patron> Patrons { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<MatchEvent> MatchEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            var jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            builder.Entity<Match>()
                .Property(b => b.CustomGame)
                .HasConversion<string>();

            builder.Entity<MatchPlayer>()
                .HasKey(mp => new { mp.MatchId, mp.SteamId });

            builder.Entity<MatchEvent>()
                .Property(b => b.Body)
                .HasConversion(
                    b => JsonSerializer.Serialize(b, jsonSerializerOptions),
                    b => JsonSerializer.Deserialize<object>(b, jsonSerializerOptions)
                );
        }
    }
}
