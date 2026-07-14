using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Myrmex.Identity.Persistence;

namespace Myrmex.WebApp.Identity;

public static class IdentityWebAppAccountEndpoints
{
    public static IEndpointRouteBuilder MapMyrmexIdentityWebAppAccountEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost(
                "/account/login-submit",
                LoginAsync)
            .AllowAnonymous();

        endpoints.MapPost(
                "/account/logout-submit",
                LogoutAsync)
            .RequireAuthorization();

        return endpoints;
    }

    internal static string NormalizeReturnUrl(string? returnUrl) =>
        IsSafeLocalReturnUrl(returnUrl) ? returnUrl! : "/";

    private static async Task<IResult> LoginAsync(
        HttpContext context,
        SignInManager<AppUser> signInManager,
        IAntiforgery antiforgery)
    {
        await antiforgery.ValidateRequestAsync(context);

        IFormCollection form = await context.Request.ReadFormAsync(
            context.RequestAborted);
        string userNameOrEmail = form["UserNameOrEmail"].ToString().Trim();
        string password = form["Password"].ToString();
        bool rememberMe = form["RememberMe"].Any(value =>
            string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "on", StringComparison.OrdinalIgnoreCase));
        string returnUrl = NormalizeReturnUrl(form["ReturnUrl"].ToString());

        if (!string.IsNullOrWhiteSpace(userNameOrEmail) &&
            !string.IsNullOrEmpty(password))
        {
            AppUser? user =
                await signInManager.UserManager.FindByEmailAsync(userNameOrEmail) ??
                await signInManager.UserManager.FindByNameAsync(userNameOrEmail);

            if (user is not null)
            {
                SignInResult result = await signInManager.PasswordSignInAsync(
                    user,
                    password,
                    rememberMe,
                    lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    return Results.LocalRedirect(returnUrl);
                }
            }
        }

        string failedUrl = "/account/login?loginFailed=true";
        if (returnUrl != "/")
        {
            failedUrl += $"&returnUrl={Uri.EscapeDataString(returnUrl)}";
        }

        return Results.LocalRedirect(failedUrl);
    }

    private static async Task<IResult> LogoutAsync(
        HttpContext context,
        SignInManager<AppUser> signInManager,
        IAntiforgery antiforgery)
    {
        await antiforgery.ValidateRequestAsync(context);

        IFormCollection form = await context.Request.ReadFormAsync(
            context.RequestAborted);
        string returnUrl = NormalizeReturnUrl(form["ReturnUrl"].ToString());

        await signInManager.SignOutAsync();

        return Results.LocalRedirect(returnUrl);
    }

    private static bool IsSafeLocalReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return false;
        }

        return returnUrl[0] == '/' &&
            (returnUrl.Length == 1 ||
                (returnUrl[1] != '/' && returnUrl[1] != '\\'));
    }
}
