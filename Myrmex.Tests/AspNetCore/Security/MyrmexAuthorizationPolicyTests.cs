using Microsoft.AspNetCore.Authorization;
using Myrmex.AspNetCore.Security;
using Myrmex.Shared.Identity;
using System.Security.Claims;

namespace Myrmex.Tests.AspNetCore.Security;

public sealed class MyrmexAuthorizationPolicyTests
{
    private const string ValidUserId = "639d86e2-969f-4b2c-8434-2473bb8eb27c";

    [Theory]
    [InlineData(false, ValidUserId, IdentityRoleNames.MyrmexAdmin, false, false)]
    [InlineData(true, null, IdentityRoleNames.MyrmexAdmin, false, false)]
    [InlineData(true, "not-a-guid", IdentityRoleNames.WmsOperator, false, false)]
    [InlineData(true, "00000000-0000-0000-0000-000000000000", IdentityRoleNames.WmsOperator, false, false)]
    [InlineData(true, ValidUserId, null, false, false)]
    [InlineData(true, ValidUserId, IdentityRoleNames.WmsOperator, true, false)]
    [InlineData(true, ValidUserId, IdentityRoleNames.MyrmexAdmin, true, true)]
    public async Task PoliciesRequireStableUserIdAndEligibleRole(
        bool authenticated,
        string? userId,
        string? role,
        bool expectedWmsOperator,
        bool expectedMyrmexAdmin)
    {
        using ServiceProvider provider = CreateServiceProvider();
        IAuthorizationService authorization = provider
            .GetRequiredService<IAuthorizationService>();
        ClaimsPrincipal principal = CreatePrincipal(authenticated, userId, role);

        AuthorizationResult wmsOperator = await authorization.AuthorizeAsync(
            principal,
            resource: null,
            MyrmexAuthorizationPolicies.WmsOperator);
        AuthorizationResult myrmexAdmin = await authorization.AuthorizeAsync(
            principal,
            resource: null,
            MyrmexAuthorizationPolicies.MyrmexAdmin);

        Assert.Equal(expectedWmsOperator, wmsOperator.Succeeded);
        Assert.Equal(expectedMyrmexAdmin, myrmexAdmin.Succeeded);
    }

    private static ServiceProvider CreateServiceProvider()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddAuthorizationBuilder()
            .AddPolicy(
                MyrmexAuthorizationPolicies.WmsOperator,
                MyrmexAuthorizationPolicies.ConfigureWmsOperator)
            .AddPolicy(
                MyrmexAuthorizationPolicies.MyrmexAdmin,
                MyrmexAuthorizationPolicies.ConfigureMyrmexAdmin);
        return services.BuildServiceProvider();
    }

    private static ClaimsPrincipal CreatePrincipal(
        bool authenticated,
        string? userId,
        string? role)
    {
        List<Claim> claims = [];
        if (userId is not null)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        }

        if (role is not null)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        ClaimsIdentity identity = authenticated
            ? new ClaimsIdentity(claims, authenticationType: "Test")
            : new ClaimsIdentity(claims);
        return new ClaimsPrincipal(identity);
    }
}
