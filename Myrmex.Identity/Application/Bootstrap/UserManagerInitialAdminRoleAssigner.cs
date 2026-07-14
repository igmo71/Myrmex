using Microsoft.AspNetCore.Identity;
using Myrmex.Identity.Persistence;

namespace Myrmex.Identity.Application.Bootstrap;

public sealed class UserManagerInitialAdminRoleAssigner(
    UserManager<AppUser> userManager)
    : IInitialAdminRoleAssigner
{
    public async Task<IdentityResult> AddToRoleAsync(
        AppUser user,
        string role,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IdentityResult result = await userManager.AddToRoleAsync(user, role);
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }
}
