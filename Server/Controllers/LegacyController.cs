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
            // var time = DateTime.UtcNow;
            // if (time.DayOfWeek != DayOfWeek.Saturday) return null;
            // var tomorrow = time.AddDays(1).Date;
            // return (tomorrow - time).TotalHours;
            return null;
        }
    }
}
