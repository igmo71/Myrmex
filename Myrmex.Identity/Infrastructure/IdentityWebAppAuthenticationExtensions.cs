using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Myrmex.AspNetCore.Security;

namespace Myrmex.Identity.Infrastructure;

public static class IdentityWebAppAuthenticationExtensions
{
    public static IServiceCollection AddMyrmexIdentityWebAppAuthentication(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = MyrmexAuthenticationSchemes.WebAppIdentity;
            options.DefaultAuthenticateScheme = MyrmexAuthenticationSchemes.WebAppIdentity;
            options.DefaultChallengeScheme = MyrmexAuthenticationSchemes.WebAppIdentity;
            options.DefaultSignInScheme = MyrmexAuthenticationSchemes.WebAppIdentity;
            options.DefaultSignOutScheme = MyrmexAuthenticationSchemes.WebAppIdentity;
            options.DefaultForbidScheme = MyrmexAuthenticationSchemes.WebAppIdentity;
        })
        .AddCookie(
            MyrmexAuthenticationSchemes.WebAppIdentity,
            options =>
            {
                options.Cookie.Name = MyrmexAuthenticationSchemes.WebAppIdentityCookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.LoginPath = "/account/login";
                options.LogoutPath = "/account/logout";
                options.AccessDeniedPath = "/account/access-denied";
            });

        return services;
    }
}
