using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Myrmex.AspNetCore.Security;
using Myrmex.Core.Application.Security;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Myrmex.Tests.Testing;

internal static class TestAuthentication
{
    private const string Scheme = "Test";

    public static IServiceCollection AddTestAuthentication(
        this IServiceCollection services,
        bool authenticated = true,
        string? actorId = "actor-sub",
        bool useSubjectClaim = false)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IActorContext, HttpContextActorContext>();
        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = Scheme;
                options.DefaultChallengeScheme = Scheme;
            })
            .AddScheme<TestAuthenticationOptions, TestAuthenticationHandler>(
                Scheme,
                options =>
                {
                    options.Authenticated = authenticated;
                    options.ActorId = actorId;
                    options.UseSubjectClaim = useSubjectClaim;
                });
        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                MyrmexAuthorizationPolicies.WmsOperator,
                MyrmexAuthorizationPolicies.ConfigureWmsOperator);
        });

        return services;
    }

    public static WebApplication UseTestAuthentication(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }

    private sealed class TestAuthenticationOptions : AuthenticationSchemeOptions
    {
        public bool Authenticated { get; set; }

        public string? ActorId { get; set; }

        public bool UseSubjectClaim { get; set; }
    }

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<TestAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<TestAuthenticationOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Options.Authenticated)
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            List<Claim> claims = [];
            if (Options.ActorId is not null)
            {
                claims.Add(new Claim(
                    Options.UseSubjectClaim ? "sub" : ClaimTypes.NameIdentifier,
                    Options.ActorId));
            }

            ClaimsPrincipal principal = new(
                new ClaimsIdentity(claims, Scheme.Name));
            AuthenticationTicket ticket = new(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
