using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Myrmex.AspNetCore.Security;

public sealed class DevelopmentActorAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration,
    IHostEnvironment environment)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string ConfigurationSectionName = "Myrmex:DevelopmentActor";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        bool enabled = Configuration.GetValue<bool>($"{ConfigurationSectionName}:Enabled");
        if (!enabled || (!Environment.IsDevelopment() && !Environment.IsStaging()))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        string? actorId = Configuration[$"{ConfigurationSectionName}:ActorId"]?.Trim();
        if (string.IsNullOrWhiteSpace(actorId))
        {
            return Task.FromResult(AuthenticateResult.Fail(
                $"{ConfigurationSectionName}:ActorId must be configured when development actor authentication is enabled."));
        }

        Claim[] claims =
        [
            new("sub", actorId),
            new(ClaimTypes.NameIdentifier, actorId),
            new(ClaimTypes.Name, actorId)
        ];
        ClaimsIdentity identity = new(claims, Scheme.Name);
        ClaimsPrincipal principal = new(identity);
        AuthenticationTicket ticket = new(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private IConfiguration Configuration { get; } = configuration;

    private IHostEnvironment Environment { get; } = environment;
}
