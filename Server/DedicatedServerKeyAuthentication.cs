using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Server
{
    public class DedicatedServerKeyAuthenticationOptions : AuthenticationSchemeOptions
    {
        public const string DefaultScheme = "Dedicated Server Key";
        public string Scheme => DefaultScheme;
        public string AuthenticationType = DefaultScheme;
    }

    public class DedicatedServerKeyAuthenticationHandler : AuthenticationHandler<DedicatedServerKeyAuthenticationOptions>
    {
        private readonly string[] _dedicatedServerKeys;
        public DedicatedServerKeyAuthenticationHandler(
            IOptionsMonitor<DedicatedServerKeyAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IConfiguration configuration
        ) : base(options, logger, encoder, clock)
        {
            _dedicatedServerKeys = configuration["DedicatedServerKeys"].Split(',');
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("Dedicated-Server-Key", out var apiKeyHeaderValues))
                return Task.FromResult(AuthenticateResult.NoResult());

            var key = apiKeyHeaderValues.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(key))
                return Task.FromResult(AuthenticateResult.NoResult());

            if (!_dedicatedServerKeys.Contains(key))
                return Task.FromResult(AuthenticateResult.Fail($"Invalid Dedicated Server Key: {key}"));

            var identity = new ClaimsIdentity(Options.AuthenticationType);
            var identities = new List<ClaimsIdentity> { identity };
            var principal = new ClaimsPrincipal(identities);
            var ticket = new AuthenticationTicket(principal, Options.Scheme);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    public static class DedicatedServerKeyAuthenticationExtensions
    {
        public static AuthenticationBuilder AddDedicatedServerKey(this AuthenticationBuilder builder) =>
            builder.AddScheme<DedicatedServerKeyAuthenticationOptions, DedicatedServerKeyAuthenticationHandler>(
                DedicatedServerKeyAuthenticationOptions.DefaultScheme,
                options => { });
    }
}
