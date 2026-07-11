using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Myrmex.AspNetCore.Security;
using Myrmex.Identity.Infrastructure.Configuration;

namespace Myrmex.Identity.Infrastructure;

public static class IdentityApiAuthenticationExtensions
{
    public static IServiceCollection AddMyrmexIdentityApiAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        IdentityDataProtectionOptions identityOptions =
            IdentityDataProtectionOptions.FromConfiguration(configuration);
        if (identityOptions.ApiSession.LifetimeMinutes !=
            IdentityDataProtectionOptions.RequiredApiSessionLifetimeMinutes)
        {
            throw new InvalidOperationException(
                "The API-session cookie lifetime must be exactly two minutes.");
        }

        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = MyrmexAuthenticationSchemes.ApiSession;
                options.DefaultAuthenticateScheme = MyrmexAuthenticationSchemes.ApiSession;
                options.DefaultChallengeScheme = MyrmexAuthenticationSchemes.ApiSession;
                options.DefaultSignInScheme = MyrmexAuthenticationSchemes.ApiSession;
                options.DefaultSignOutScheme = MyrmexAuthenticationSchemes.ApiSession;
                options.DefaultForbidScheme = MyrmexAuthenticationSchemes.ApiSession;
            })
            .AddCookie(
                MyrmexAuthenticationSchemes.ApiSession,
                options =>
                {
                    options.Cookie.Name = MyrmexAuthenticationSchemes.ApiSessionCookieName;
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SameSite = SameSiteMode.Strict;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(
                        identityOptions.ApiSession.LifetimeMinutes);
                    options.SlidingExpiration = false;
                    options.Events = new CookieAuthenticationEvents
                    {
                        OnRedirectToLogin = context =>
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            return Task.CompletedTask;
                        },
                        OnRedirectToAccessDenied = context =>
                        {
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            return Task.CompletedTask;
                        }
                    };
                });

        return services;
    }
}
