using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Server.Models;

namespace Server.Pages
{
    public class SupportersModel : PageModel
    {
        private readonly AppDbContext _context;
        public SupportersModel(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnPost(ulong steamId, ushort level, string comment)
        {
            if (!ModelState.IsValid) return BadRequest();

            var player = await _context.Players.FindOrCreatePlayer(steamId);

            if (player.PatreonEndDate > DateTime.UtcNow)
                return BadRequest($"Player {steamId} already has a managed supporter state");

            player.Comment = comment;
            player.PatreonLevel = level;
            player.PatreonEndDate = null;

            await _context.SaveChangesAsync();

            return Redirect(Request.GetDisplayUrl());
        }

        public async Task<Supporter[]> GetAllSupporters() =>
            await _context.Players
                .Where(p => p.PatreonLevel > 0 && (!p.PatreonEndDate.HasValue || p.PatreonEndDate > DateTime.UtcNow))
                .Select(p => new Supporter()
                {
                    SteamId = p.SteamId,
                    Comment = p.Comment,
                    Level = p.PatreonLevel,
                    EndDate = p.PatreonEndDate,
                })
                .OrderBy(p => p.EndDate ?? DateTime.MinValue)
                .ThenBy(p => p.SteamId)
                .ToArrayAsync();

        public async Task<int> GetExpiredSupporterCount() =>
            await _context.Players.CountAsync(p => p.PatreonLevel > 0 && p.PatreonEndDate < DateTime.UtcNow);
    }

    public class Supporter
    {
        public ulong SteamId { get; set; }
        public string Comment { get; set; }
        public ushort Level { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
