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

        string rawBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            $"Expected 200 OK, got {(int)response.StatusCode} {response.StatusCode}. Body: {rawBody}");

        var body = await IdentitySessionBoundaryFixture.ReadActorAsync(response);

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
