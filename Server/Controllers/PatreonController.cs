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
                            var premium = new Premium
                            {
                                Patreon = patreonUser
                            };
                            _context.Add(premium);
                            premium.Verify(Request.Host.ToString());
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
    [Key] public int Id { get; set; }
    public int SteamId { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;



}

public class PatreonUser
{
    [Key] public int Id { get; set; }
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
    [Key] public int Id { get; set; }
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

public class Premium
{
    [Key] public int Id { get; set; }
    public Steam Steam { get; set; } = new Steam();

    public Verification Verification { get; set; } = new Verification();

    public PatreonUser Patreon { get; set; } = null;

    public Boolean Active { get; set; }

    public int Tier {
        get {
            return calculateTier();
        }
    }
    public int FriendInvites { 
        get {
            return calculateInvites();
        }
    }

    public Boolean Invited { get; set; } = false;

    public string Email { get; set; }


    public int calculateInvites()
    {
        if (Patreon != null)
        {
            double amount = Patreon.amount_cents / 100.0;
            if (amount >= 10.0 && amount < 25.0)
            {
                return 1;
            }
            if (amount >= 25.0 && amount < 50.0)
            {
                return 3;
            }
            if (amount >= 50)
            {
                return 5;
            }
        }
        return 0;
    }
    public int calculateTier()
    {
        if (Patreon != null)
        {
            double amount = Patreon.amount_cents / 100.0;
            if (amount <= 10.0)
            {
                return 1;
            }
            if (amount >= 25.0)
            {
                return 2;
            }

        }
        if (Invited)
        {
            return 1;
        }
        return 0;
    }
    public void SendActivation(string Host)
    {
        SendMail(
                Email,
                String.Format("Dota12v12: You are now Premium! Activate your Steam Account"),
                String.Format(@"
                Thank you for your Support!
            
                To receive all our perks, Please active your steam account with the link below.

                https://{0}/activate?g={1}

                ", Host, Verification.Guid)
        );
        Verification.Status = 1;
    }

    public void SendInvite(string Host, string email)
    {
        var invitee = new Premium
        {
            Invited = true,
            Email = email
        };

        SendMail(
                email,
                String.Format(@"Dota12v12: {0} Gifted you a premium account", Steam.Name),
                String.Format(@"
                    To receive all our perks, Please active your steam account with the link below.

                    https://{0}/activate?g={1}

                ", Host, invitee.Verification.Guid)
        );
        invitee.Verification.Status = 1;
    }

    public void SendMail(string to_email, string subject, string body)
    {
        MailMessage mail = new MailMessage();
        SmtpClient SmtpServer = new SmtpClient(SMTP.Host);
        mail.From = new MailAddress(SMTP.From);
        mail.To.Add(to_email);
        mail.Subject = subject;
        mail.Body = body;
        SmtpServer.Port = SMTP.Port;
        SmtpServer.EnableSsl = SMTP.SSL;
        SmtpServer.Credentials = new System.Net.NetworkCredential(SMTP.Username, SMTP.Password);
        SmtpServer.Send(mail);

    }
}
class WebhookResponse
{

}