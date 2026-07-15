using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Myrmex.Identity.Persistence;
using System.Security.Claims;

namespace Myrmex.WebApp.Identity;

public class IdentityRevalidatingAuthenticationStateProvider(
    IServiceScopeFactory scopeFactory,
    ILoggerFactory loggerFactory)
    : RevalidatingServerAuthenticationStateProvider(loggerFactory)
{
    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(30);

    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState,
        CancellationToken cancellationToken)
    {
        ClaimsPrincipal principal = authenticationState.User;
        if (principal.Identity?.IsAuthenticated != true ||
            !HasSingleStableIdentityUserId(principal))
        {
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();

        using IServiceScope scope = scopeFactory.CreateScope();
        SignInManager<AppUser> signInManager = scope.ServiceProvider
            .GetRequiredService<SignInManager<AppUser>>();

        AppUser? user = await signInManager.ValidateSecurityStampAsync(
            principal);

        cancellationToken.ThrowIfCancellationRequested();

        return user is not null;
    }

    private static bool HasSingleStableIdentityUserId(ClaimsPrincipal principal)
    {
        string[] userIds = principal.FindAll(ClaimTypes.NameIdentifier)
            .Select(claim => claim.Value)
            .ToArray();

        return userIds.Length == 1 &&
            Guid.TryParse(userIds[0], out Guid userId) &&
            userId != Guid.Empty;
    }
}
