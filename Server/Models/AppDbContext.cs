using Microsoft.EntityFrameworkCore;

namespace Server.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Player> Players { get; set; }
        public DbSet<Match> Matches { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<MatchPlayer>().HasKey(mp => new { mp.MatchId, mp.SteamId });
        }
    }
}
