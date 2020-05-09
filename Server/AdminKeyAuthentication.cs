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
    public class AdminKeyAuthenticationOptions : AuthenticationSchemeOptions
    {
        public const string DefaultScheme = "Admin Key";
        public string Scheme => DefaultScheme;
        public string AuthenticationType = DefaultScheme;
    }

    public class AdminKeyAuthenticationHandler : AuthenticationHandler<AdminKeyAuthenticationOptions>
    {
        private readonly string _key;
        public AdminKeyAuthenticationHandler(
            IOptionsMonitor<AdminKeyAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IConfiguration configuration
        ) : base(options, logger, encoder, clock)
        {
            _key = configuration["AdminKey"];
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var key = Request.Query["key"].FirstOrDefault();
            if (key != _key)
                return Task.FromResult(AuthenticateResult.Fail($"Invalid Admin Key: {key}"));

            var identity = new ClaimsIdentity(Options.AuthenticationType);
            var identities = new List<ClaimsIdentity> { identity };
            var principal = new ClaimsPrincipal(identities);
            var ticket = new AuthenticationTicket(principal, Options.Scheme);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    public static class AdminKeyAuthenticationExtensions
    {
        public static AuthenticationBuilder AddAdminKey(this AuthenticationBuilder builder) =>
            builder.AddScheme<AdminKeyAuthenticationOptions, AdminKeyAuthenticationHandler>(
                AdminKeyAuthenticationOptions.DefaultScheme,
                options => { });
    }
}
