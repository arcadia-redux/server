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
    public class ActivateModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly string _key;

        public ActivateModel(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
        }
        public Premium Validate()
        {
            if (Request.Query["g"].ToString().Length == 0)
            {
                return null;
            }
            var premium = _context.Premiums.Where(p => p.Verification.Guid.ToString() == Request.Query["g"].ToString()).First();
            if (premium != null)
            {
                return premium;
            }
            return null;
        }

        public void OnGet()
        {

        }
    }

    class UserAccount { 
        public int Id { get; set; }
        public int Invites { get; set; }
    }


}