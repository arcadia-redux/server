using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Server.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.Json;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PatreonController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger _logger;
        public PatreonController(AppDbContext context, ILogger<MatchController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        [Route("patreon-webhook")]
        public async Task<ActionResult> PatreonWebhook([FromBody] JsonData request)
        {
            string evnt = Request.Headers["X-Patreon-Event"];
            Included includes = request.included;
            Patron existing;

            if (includes.patrons.Count() > 0)
            {
                var patreonUser = includes.patrons[0];
                
                switch (evnt){
                    case "pledges:create":
                        existing = _context.Patrons.Where(p => p.Patreon._id == patreonUser._id).First();
                        if (existing == null)
                        {
                            var patron = new Patron
                            {
                                Patreon = patreonUser
                            };
                            _context.Add(patron);
                            patron.Verify(Request.Host.ToString());
                        } else
                        {
                            existing.Active = true; 
                        }

                        await _context.SaveChangesAsync();
                        break;
                    case "pledges:update":
                        existing = _context.Patrons.Where(p => p.Patreon._id == patreonUser._id).First();
                        if (existing != null)
                        {
                            existing.Active = true;
                            existing.Patreon.amount_cents = patreonUser.amount_cents;

                            await _context.SaveChangesAsync();
                        }
                        break;
                    case "pledges:delete":
                        existing = _context.Patrons.Where(p => p.Patreon._id == patreonUser._id).First();
                        if (existing != null)
                        {
                            existing.Active = false;
                            await _context.SaveChangesAsync();
                        }
                        break;
                }
            }
            return Ok();
        }
    }

}
public class JsonData
{
    public Included included;

}
public class Included
{
    public List<PatreonUser> patrons;
}

public class Steam
{
    public int id { get; set; }
    public DateTime created_at { get; set; } = DateTime.UtcNow;
    public DateTime updated_at { get; set; } = DateTime.UtcNow;



}

public class PatreonUser
{
    public int _id { get; set; }
    public DateTime created_at { get; set; } = DateTime.UtcNow;
    public DateTime updated_at { get; set; } = DateTime.UtcNow;
    public string full_name { get; set; }
    public string first_name { get; set; }
    public string last_name { get; set; }
    public string email { get; set; }
    public string image_url { get; set; }
    public double amount_cents { get; set; }


}
public class Verification
{
    public int Status { get; set; } = 0;

    public Guid Guid { get; set; } = Guid.NewGuid();


}

public static class SMTP
{
    public static string Host = "smtp.gmail.com";
    public static int Port = 587;
    public static string From = "your_email_address@gmail.com";
    public static string Username = "";
    public static string Password = "";
    public static Boolean SSL = true;
}

public class Patron
{
    public Steam Steam { get; set; } = new Steam();

    public Verification Verification { get; set; } = new Verification();

    public PatreonUser Patreon { get; set; }

    public Boolean Active { get; set; }

    public int FriendInvites;

    public void Verify(string Host)
    {
        MailMessage mail = new MailMessage();
        SmtpClient SmtpServer = new SmtpClient(SMTP.Host);

        mail.From = new MailAddress(SMTP.From);
        mail.To.Add(Patreon.email);
        mail.Subject = "Dota12v12: Thank you for your Support! Activate your Steam Account";
        mail.Body = String.Format(@"
            Thank you for your Support!
            
            To receive all our perks, Please active your steam account with the link below.

            https://{0}/verify?g={1}

        ", Host, Verification.Guid);

        SmtpServer.Port = SMTP.Port;
        SmtpServer.EnableSsl = SMTP.SSL;
        SmtpServer.Credentials = new System.Net.NetworkCredential(SMTP.Username, SMTP.Password);

        SmtpServer.Send(mail);

        Verification.Status = 1;
    }
}
class WebhookResponse
{

}