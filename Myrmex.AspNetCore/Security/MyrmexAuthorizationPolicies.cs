using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Myrmex.AspNetCore.Security;

public static class MyrmexAuthorizationPolicies
{
    public const string WmsOperator = "WmsOperator";

    public static void ConfigureWmsOperator(AuthorizationPolicyBuilder policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context => context.User.Claims.Any(claim =>
            (claim.Type == ClaimTypes.NameIdentifier || claim.Type == "sub") &&
            !string.IsNullOrWhiteSpace(claim.Value)));
    }
}
