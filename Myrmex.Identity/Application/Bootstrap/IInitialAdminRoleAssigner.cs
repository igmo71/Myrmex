using Microsoft.AspNetCore.Identity;
using Myrmex.Identity.Persistence;

namespace Myrmex.Identity.Application.Bootstrap;

public interface IInitialAdminRoleAssigner
{
    Task<IdentityResult> AddToRoleAsync(
        AppUser user,
        string role,
        CancellationToken cancellationToken);
}
