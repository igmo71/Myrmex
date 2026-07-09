using Myrmex.Shared.Identity;

namespace Myrmex.Tests.Identity;

public sealed class IdentityApiSessionAuthenticationTests
{
    [Fact]
    public async Task ValidApiSessionCookie_AuthenticatesProtectedApiServiceEndpoint()
    {
        await using IdentitySessionBoundaryFixture fixture =
            await IdentitySessionBoundaryFixture.CreateAsync();
        var user = await fixture.CreateUserAsync(IdentityRoleNames.WmsOperator);

        using HttpResponseMessage response = await fixture.SendForUserAsync(user.Id);
        var body = await IdentitySessionBoundaryFixture.ReadActorAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(user.Id.ToString(), body!.UserId);
        Assert.Equal(user.Id.ToString(), body.ActorId);
        Assert.Contains(IdentityRoleNames.WmsOperator, body.Roles);
    }

    [Fact]
    public async Task BrowserApplicationCookie_DoesNotAuthenticateProtectedApiServiceEndpoint()
    {
        await using IdentitySessionBoundaryFixture fixture =
            await IdentitySessionBoundaryFixture.CreateAsync();
        string validApiSessionTicket = fixture.ProtectTicket();

        using HttpResponseMessage response = await fixture.SendBrowserCookieAsync(validApiSessionTicket);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
