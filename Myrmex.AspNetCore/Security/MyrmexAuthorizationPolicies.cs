using Microsoft.AspNetCore.Authorization;
using Myrmex.Shared.Identity;
using System.Security.Claims;

namespace Myrmex.AspNetCore.Security;

public static class MyrmexAuthorizationPolicies
{
    public const string WmsOperator = IdentityRoleNames.WmsOperator;

    public const string MyrmexAdmin = IdentityRoleNames.MyrmexAdmin;

    public static void ConfigureWmsOperator(AuthorizationPolicyBuilder policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context => HasStableIdentityUserId(context.User));
        policy.RequireRole(
            IdentityRoleNames.WmsOperator,
            IdentityRoleNames.MyrmexAdmin);
    }

    public static void ConfigureMyrmexAdmin(AuthorizationPolicyBuilder policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context => HasStableIdentityUserId(context.User));
        policy.RequireRole(IdentityRoleNames.MyrmexAdmin);
    }

    private static bool HasStableIdentityUserId(ClaimsPrincipal principal)
    {
        string[] userIds = principal.FindAll(ClaimTypes.NameIdentifier)
            .Select(claim => claim.Value)
            .ToArray();

        return userIds.Length == 1 &&
            Guid.TryParse(userIds[0], out Guid userId) &&
            userId != Guid.Empty;
    }
}
