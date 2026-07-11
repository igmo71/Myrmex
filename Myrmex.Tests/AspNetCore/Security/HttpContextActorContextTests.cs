using Microsoft.AspNetCore.Http;
using Myrmex.AspNetCore.Security;
using System.Security.Claims;

namespace Myrmex.Tests.AspNetCore.Security;

public sealed class HttpContextActorContextTests
{
    [Fact]
    public void ActorId_WithStableIdentityUserId_ReturnsUserId()
    {
        Guid userId = Guid.NewGuid();
        HttpContextActorContext actorContext = CreateActorContext(
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()));

        string actorId = actorContext.ActorId;

        Assert.Equal(userId.ToString(), actorId);
    }

    [Theory]
    [InlineData(ClaimTypes.Email, "operator@example.com")]
    [InlineData(ClaimTypes.Name, "Warehouse Operator")]
    [InlineData("sub", "639d86e2-969f-4b2c-8434-2473bb8eb27c")]
    public void ActorId_WithMutableOrNonIdentityClaim_Throws(
        string claimType,
        string claimValue)
    {
        HttpContextActorContext actorContext = CreateActorContext(
            new Claim(claimType, claimValue));

        Assert.Throws<InvalidOperationException>(() => actorContext.ActorId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void ActorId_WithMissingOrInvalidUserId_Throws(string? userId)
    {
        HttpContextActorContext actorContext = userId is null
            ? CreateActorContext()
            : CreateActorContext(new Claim(ClaimTypes.NameIdentifier, userId));

        Assert.Throws<InvalidOperationException>(() => actorContext.ActorId);
    }

    private static HttpContextActorContext CreateActorContext(params Claim[] claims)
    {
        DefaultHttpContext httpContext = new()
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
        };
        HttpContextAccessor accessor = new() { HttpContext = httpContext };
        return new HttpContextActorContext(accessor);
    }
}
