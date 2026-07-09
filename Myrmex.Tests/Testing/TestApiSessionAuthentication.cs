using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Myrmex.AspNetCore.Security;
using Myrmex.Core.Application.Security;
using Myrmex.Identity.Infrastructure;
using System.Security.Claims;

namespace Myrmex.Tests.Testing;

internal static class TestApiSessionAuthentication
{
    private const string ApplicationName = "Myrmex.Tests.ApiSession";

    public static IServiceCollection AddTestApiSessionAuthentication(
        this IServiceCollection services)
    {
        services.AddDataProtection()
            .SetApplicationName(ApplicationName);
        services.AddHttpContextAccessor();
        services.AddScoped<IActorContext, HttpContextActorContext>();
        services.AddMyrmexIdentityApiAuthentication(CreateConfiguration());
        services.AddAuthorizationBuilder()
            .AddPolicy(
                MyrmexAuthorizationPolicies.WmsOperator,
                MyrmexAuthorizationPolicies.ConfigureWmsOperator)
            .AddPolicy(
                MyrmexAuthorizationPolicies.MyrmexAdmin,
                MyrmexAuthorizationPolicies.ConfigureMyrmexAdmin);

        return services;
    }

    public static string CreateApiSessionCookie(
        this IServiceProvider services,
        IReadOnlyCollection<string>? roles = null,
        Guid? userId = null)
    {
        Guid actorId = userId ?? Guid.NewGuid();
        IReadOnlyCollection<string> sessionRoles = roles ?? Array.Empty<string>();
        Claim[] claims =
        [
            new Claim(ClaimTypes.NameIdentifier, actorId.ToString()),
            .. sessionRoles.Select(role => new Claim(ClaimTypes.Role, role))
        ];
        DateTimeOffset issuedUtc = DateTimeOffset.UtcNow;
        AuthenticationTicket ticket = new(
            new ClaimsPrincipal(new ClaimsIdentity(
                claims,
                MyrmexAuthenticationSchemes.ApiSession,
                ClaimTypes.Name,
                ClaimTypes.Role)),
            new AuthenticationProperties
            {
                IssuedUtc = issuedUtc,
                ExpiresUtc = issuedUtc.AddMinutes(2),
                IsPersistent = false,
                AllowRefresh = false
            },
            MyrmexAuthenticationSchemes.ApiSession);

        CookieAuthenticationOptions options = services
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(MyrmexAuthenticationSchemes.ApiSession);
        string protectedTicket = options.TicketDataFormat.Protect(ticket);

        return $"{MyrmexAuthenticationSchemes.ApiSessionCookieName}={protectedTicket}";
    }

    public static WebApplication UseTestApiSessionAuthentication(
        this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }

    private static IConfiguration CreateConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Myrmex:Identity:ApiSession:LifetimeMinutes"] = "2"
            })
            .Build();
}
