using System;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Server.Models;
using Server.Services;
using Stripe;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace Server.Controllers
{

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CreatePaymentProvider
    {
        Alipay,
        WeChat,
    }

    public class CreatePaymentRequest
    {
        [Required] public CreatePaymentProvider Provider { get; set; }
        [Required] public PaymentKind PaymentKind { get; set; }
        [Required] public string SteamId { get; set; }
        [Required] public long MatchId { get; set; }
    }

    public class CreatePaymentResponse
    {
        public string Url { get; set; }
    }

    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger _logger;
        private readonly StripeService _stripeService;
        private readonly string _stripeSignature;

        public PaymentController(AppDbContext context, ILogger<MatchController> logger, StripeService stripeService, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _stripeService = stripeService;
            _stripeSignature = configuration["StripeSignature"];
        }

        [HttpPost]
        [Route("create")]
        public async Task<CreatePaymentResponse> Create([FromBody] CreatePaymentRequest request)
        {
            var steamId = ulong.Parse(request.SteamId);
            var matchId = request.MatchId;
            var url = request.Provider switch
            {
                CreatePaymentProvider.Alipay => await _stripeService.CreateAlipayRequest(steamId, matchId, request.PaymentKind),
                CreatePaymentProvider.WeChat => await _stripeService.CreateWeChatRequest(steamId, matchId, request.PaymentKind),
                _ => throw new Exception($"Unknown provider kind: {request.Provider}"),
            };

            return new CreatePaymentResponse { Url = url };
        }

        [HttpPost]
        [Route("stripe")]
        [AllowAnonymous]
        public async Task<ActionResult> Stripe()
        {
            using var bodySteam = new StreamReader(HttpContext.Request.Body);
            var json = await bodySteam.ReadToEndAsync();

            var stripeEvent = EventUtility.ConstructEvent(json, Request.Headers["Stripe-Signature"], _stripeSignature);
            switch (stripeEvent.Type)
            {
                case Events.SourceChargeable:
                    var source = stripeEvent.Data.Object as Source;
                    await _stripeService.HandleSourceChargeable(source);
                    return Ok();

                case Events.ChargeSucceeded:
                    var charge = stripeEvent.Data.Object as Charge;
                    _stripeService.HandleChargeSucceeded(charge);
                    return Ok();

                default:
                    return BadRequest("Unknown event type");
            }
        }
    }
}
