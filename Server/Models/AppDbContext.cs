using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Server.Enums;
using Server.Helpers;

namespace Server.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Player> Players { get; set; }
        public DbSet<Match> Matches { get; set; }
        // TODO: Changing the collection
        public DbSet<MatchPlayer> MatchPlayer { get; set; }
        public DbSet<MatchEvent> MatchEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // TODO: Npgsql 3.0 - https://www.npgsql.org/efcore/release-notes/3.0.html#breaking-changes
            builder.UseSerialColumns();

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

            builder.Entity<Player>()
                .Property(s => s.Rating12v12)
                .HasDefaultValue(Player.DefaultRating);


            builder.Entity<Player>()
                .OwnsMany(p => p.PlayerOverthrowRating, a =>
                {
                    a.WithOwner().HasForeignKey("SteamId");
                    a.Property<ulong>("SteamId");
                    a.HasKey("SteamId", "MapName");
                });
        }
    }
}
