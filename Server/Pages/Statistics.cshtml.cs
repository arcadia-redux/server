using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Server.Models;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Server.Pages
{
    public class StatisticsModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly string _key;

        public StatisticsModel(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _key = configuration["SupportersAdminPanelKey"];
        }

        public IActionResult OnGet(string key)
        {
            if (key != _key) return Unauthorized();
            return Page();
        }

        public async Task<List<HeroEntry>> GetHeroes(CustomGame customGame)
        {
            var includeMatchesSince = DateTime.UtcNow.AddMonths(-1);

            Task<List<T>> SelectMatches<T>(bool onlyWinners, Expression<Func<IGrouping<string, MatchPlayer>, T>> selector) =>
                _context.MatchPlayer
                    .Where(mp => mp.Match.CustomGame == customGame)
                    .Where(mp => mp.Match.EndedAt > includeMatchesSince)
                    .Where(mp => !onlyWinners || mp.Team == mp.Match.Winner)
                    .GroupBy(mp => mp.Hero)
                    .Select(selector)
                    .ToListAsync();

            var wins = await SelectMatches(true, g => new { Hero = g.Key, Wins = g.Count() });
            var totals = await SelectMatches(false, g => new { Hero = g.Key, Total = g.Count() });

            return totals
                .Select(e => new HeroEntry()
                {
                    Hero = e.Hero,
                    WinRate = Math.Round((double)(wins.FirstOrDefault(e2 => e2.Hero == e.Hero)?.Wins ?? 0) / e.Total * 100, 2),
                    TotalMatches = e.Total,
                })
                .OrderByDescending(e => e.WinRate)
                .ToList();
        }
    }

    public class HeroEntry
    {
        [DisplayName("Hero Name")]
        public string Hero { get; set; }
        [DisplayName("Win Rate")]
        public double WinRate { get; set; }
        [DisplayName("Total matches")]
        public int TotalMatches { get; set; }
    }
}
