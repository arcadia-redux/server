using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Server.Models;

namespace Server.Controllers
{
    [ApiController]
    public class LegacyController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LegacyController(AppDbContext context)
        {
            _context = context;
        }


        [Route("/api/same-hero-day")]
        public ActionResult<double?> GetSameHeroDayHoursLeft()
        {
//            var time = DateTime.UtcNow;
//            if (time.DayOfWeek != DayOfWeek.Saturday) return null;
//            var tomorrow = time.AddDays(1).Date;
//            return (tomorrow - time).TotalHours;
            return null;
        }

        [HttpGet("/api/players")]
        public ActionResult<List<LegacyPlayerResponse>> GetAll([FromQuery(Name = "id")] ulong[] ids,
            [FromQuery(Name = "map")] string mapName)
        {
            if (ids.GroupBy(id => id).Any(id => id.Count() > 1))
            {
                return BadRequest("Duplicates in ids are not allowed");
            }

            if (ids.Length > 24)
            {
                return BadRequest("Too much ids requested");
            }

            var players = _context.Players.Where(p => ids.Contains(p.SteamId))
                .Select(p => new {p.SteamId, p.PatreonLevel})
                .ToArray();

            return ids.Select(id =>
                {
                    if (players.All(p => p.SteamId != id)) return new LegacyPlayerResponse() {SteamId = id.ToString()};

                    var o = players.First(p => p.SteamId == id);
                    return new LegacyPlayerResponse {SteamId = id.ToString(), PatreonLevel = o.PatreonLevel};
                })
                .ToList();
        }
    }

    public class LegacyPlayerResponse
    {
        public string SteamId { get; set; }
        public ushort PatreonLevel { get; set; }
    }
}
