using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Server.Models;

namespace Server.Pages
{
    public class SupportersAdminPanelModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly string _key;

        public SupportersAdminPanelModel(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _key = configuration["SupportersAdminPanelKey"];
        }

        public IActionResult OnGet(string key)
        {
            if (key != _key) return Unauthorized();
            return Page();
        }

        [BindProperty]
        public SetPatreonLevelRequest Req { get; set; }

        public IActionResult OnPost(string key)
        {
            if (key != _key) return Unauthorized();
            if (!ModelState.IsValid) return Page();

            var player = _context.Players.FirstOrDefault(p => p.SteamId == Req.SteamId);
            if (player == null)
            {
                player = new Player() { SteamId = Req.SteamId };
                _context.Add(player);
            }
            player.PatreonLevel = Req.PatreonLevel;
            player.Comment = Req.Comment;
            _context.SaveChanges();
            return Page();
        }

        public async Task<PatreonPlayer[]> GetAllSupporters()
        {
            return await _context.Players
                .Where(p => p.PatreonLevel > 0)
                .Select(p => new PatreonPlayer()
                {
                    SteamId = p.SteamId,
                    Comment = p.Comment,
                    PatreonLevel = p.PatreonLevel,
                    PatreonEndDate = p.PatreonEndDate,
                })
                .ToArrayAsync();
        }
    }

    public class PatreonPlayer
    {
        public ulong SteamId { get; set; }
        public string Comment { get; set; }
        public ushort PatreonLevel { get; set; }
        public DateTime? PatreonEndDate { get; set; }
    }

    public class SetPatreonLevelRequest
    {
        [DisplayName("Steam ID")]
        public ulong SteamId { get; set; }
        [DisplayName("Patreon Level")]
        public ushort PatreonLevel { get; set; }
        [DisplayName("Comment")]
        public string Comment { get; set; }
    }
}
