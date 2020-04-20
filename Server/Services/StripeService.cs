using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Server.Models;
using Stripe;

namespace Server.Services
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PaymentKind
    {
        Purchase1,
        Purchase2,
        UpgradeTo2,
    }

    public class StripeService
    {
        private readonly IConfiguration _configuration;
        private readonly SourceService _sourceService;
        private readonly ChargeService _chargeService;
        private readonly ILogger _logger;
        private readonly AppDbContext _context;
        private readonly string _virtualHost;

        public StripeService(IConfiguration configuration, ILogger<StripeService> logger, AppDbContext context)
        {
            _configuration = configuration;
            _logger = logger;
            _context = context;
            _virtualHost = configuration["VIRTUAL_HOST"];

            StripeConfiguration.ApiKey = configuration["StripeApiKey"];
            _sourceService = new SourceService();
            _chargeService = new ChargeService();
        }

        public async Task<string> CreateWeChatRequest(ulong steamId, ulong payerSteamId, long matchId, PaymentKind paymentKind)
        {
            var sourceCreateOptions = await CreateStripeSourceCreateOptions(steamId, payerSteamId, matchId, paymentKind);
            sourceCreateOptions.Type = "wechat";
            var source = await _sourceService.CreateAsync(sourceCreateOptions);

            // WeChat is in beta, so it's not typed yet
            var rawResult = JsonDocument.Parse(source.StripeResponse.Content).RootElement;
            var qrCodeUrl = rawResult.GetProperty("wechat").GetProperty("qr_code_url").GetString();
            var uri = QueryHelpers.AddQueryString($"http://{_virtualHost}/payment/wechat", "qr", qrCodeUrl);

            return uri;
        }

        public async Task<string> CreateAlipayRequest(ulong steamId, ulong payerSteamId, long matchId, PaymentKind paymentKind)
        {
            var sourceCreateOptions = await CreateStripeSourceCreateOptions(steamId, payerSteamId, matchId, paymentKind);
            sourceCreateOptions.Type = "alipay";
            var source = await _sourceService.CreateAsync(sourceCreateOptions);

            return source.Redirect.Url;
        }

        private async Task<SourceCreateOptions> CreateStripeSourceCreateOptions(ulong steamId, ulong payerSteamId, long matchId, PaymentKind paymentKind)
        {
            var player = await _context.Players.FindAsync(steamId);
            if (player == null)
            {
                player = new Player { SteamId = steamId };
                _context.Players.Add(player);
                await _context.SaveChangesAsync();
            }

            ValidatePaymentKind(player, paymentKind);

            // 1 AUD = $conversionRate CNY
            var conversionRate = 4.54665;
            // 50 CNY
            var priceTier1 = (int)Math.Floor((50 / conversionRate) * 100);
            // 200 CNY
            var priceTier2 = (int)Math.Floor((200 / conversionRate) * 100);

            var patreonDaysLeft = player.PatreonEndDate != null ? (player.PatreonEndDate - DateTime.UtcNow).Value.Days : 0;
            var amount = paymentKind switch
            {
                PaymentKind.Purchase1 => priceTier1,
                PaymentKind.Purchase2 => priceTier2,
                PaymentKind.UpgradeTo2 => priceTier2 - (int)Math.Floor(priceTier1 * ((double)patreonDaysLeft / 30)),
                _ => throw new NotImplementedException(),
            };

            var statementDescriptor = paymentKind switch
            {
                PaymentKind.Purchase1 => "Basic Dota 2 Unofficial supporter status",
                PaymentKind.Purchase2 => "Advanced Dota 2 Unofficial supporter status",
                PaymentKind.UpgradeTo2 => "Upgrade to advanced supporter tier",
                _ => throw new NotImplementedException(),
            };

            return new SourceCreateOptions
            {
                Amount = amount,
                Currency = "aud",
                StatementDescriptor = statementDescriptor,

                Redirect = new SourceRedirectOptions
                {
                    // TODO: Do we really need this?
                    ReturnUrl = $"http://{_virtualHost}/payment/loader"
                },
                Metadata = new Dictionary<string, string>
                {
                    ["steamId"] = steamId.ToString(),
                    ["payerSteamId"] = payerSteamId.ToString(),
                    ["matchId"] = matchId.ToString(),
                    ["paymentKind"] = paymentKind.ToString(),
                },
            };
        }

        public async Task HandleSourceChargeable(Source source)
        {
            var steamId = ulong.Parse(source.Metadata["steamId"]);
            var payerSteamId = ulong.Parse(source.Metadata["payerSteamId"]);
            var matchId = long.Parse(source.Metadata["matchId"]);
            try
            {
                var player = await _context.Players.FindAsync(steamId);
                if (player == null)
                {
                    player = new Player { SteamId = steamId };
                    _context.Players.Add(player);
                    await _context.SaveChangesAsync();
                }

                var paymentKind = (PaymentKind)Enum.Parse(typeof(PaymentKind), source.Metadata["paymentKind"]);
                ValidatePaymentKind(player, paymentKind);

                player.PatreonEndDate = DateTime.UtcNow.AddDays(30);
                player.PatreonLevel = paymentKind switch
                {
                    PaymentKind.Purchase1 => 1,
                    PaymentKind.Purchase2 => 2,
                    PaymentKind.UpgradeTo2 => 2,
                    _ => throw new NotImplementedException(),
                };

                var charge = await _chargeService.CreateAsync(new ChargeCreateOptions
                {
                    Amount = source.Amount,
                    Currency = source.Currency,
                    Source = source.Id,
                });

                if (charge.Status != "succeeded")
                {
                    // TODO: Throw?
                    _logger.LogCritical($"Charge {charge.Id} of source {source.Id} has invalid initial status {charge.Status}");
                }

                _context.MatchEvents.Add(new MatchEvent()
                {
                    MatchId = matchId,
                    Body = new PaymentUpdateMatchEventBody()
                    {
                        SteamId = steamId.ToString(),
                        PayerSteamId = payerSteamId.ToString(),
                        Level = player.PatreonLevel,
                        EndDate = player.PatreonEndDate,
                    }
                });

                await _context.SaveChangesAsync();

                _logger.LogCritical($"Charge for user '{steamId}' is complete!");
            }
            catch (Exception ex)
            {
                _context.MatchEvents.Add(new MatchEvent()
                {
                    MatchId = matchId,
                    Body = new PaymentUpdateMatchEventBody()
                    {
                        SteamId = steamId.ToString(),
                        PayerSteamId = payerSteamId.ToString(),
                        Error = ex.Message,
                    }
                });

                _logger.LogCritical($"Charge for user '{steamId}' failed: {ex.Message}");

                await _context.SaveChangesAsync();
            }
        }

        private void ValidatePaymentKind(Player player, PaymentKind paymentKind)
        {
            switch (paymentKind)
            {
                case PaymentKind.Purchase1:
                case PaymentKind.Purchase2:
                    // TODO: Share logic with controller
                    if (player.PatreonLevel != 0 && player.PatreonEndDate > DateTime.UtcNow)
                    {
                        throw new Exception($"Can't purchase subscription with existing level {player.PatreonLevel}");
                    }

                    break;

                case PaymentKind.UpgradeTo2:
                    if (player.PatreonEndDate == null)
                    {
                        throw new Exception("Can't upgrade subscription without existing end date");
                    }

                    if (player.PatreonEndDate < DateTime.UtcNow)
                    {
                        throw new Exception("Can't upgrade expired subscription");
                    }

                    if (player.PatreonLevel != 1)
                    {
                        throw new Exception($"Can't upgrade subscription level from {player.PatreonLevel}");
                    }

                    break;

                default:
                    throw new Exception($"Unknown payment kind {paymentKind}");
            }
        }

        public void HandleChargeSucceeded(Charge charge)
        {
            // All currently supported sources are meant to have "succeeded" status immediately
        }
    }
}
