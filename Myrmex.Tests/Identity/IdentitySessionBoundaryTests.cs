using Myrmex.Shared.Identity;

namespace Myrmex.Tests.Identity;

public sealed class IdentitySessionBoundaryTests
{
    [Theory]
    [InlineData(IdentityRoleNames.WmsOperator)]
    [InlineData(IdentityRoleNames.MyrmexAdmin)]
    public async Task CurrentEligibleRole_ReachesApiWithExactActorId(string role)
    {
        await using IdentitySessionBoundaryFixture fixture =
            await IdentitySessionBoundaryFixture.CreateAsync();
        var user = await fixture.CreateUserAsync(role);

        using HttpResponseMessage response = await fixture.SendForUserAsync(user.Id);
        var body = await IdentitySessionBoundaryFixture.ReadActorAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(user.Id.ToString(), body!.UserId);
        Assert.Equal(user.Id.ToString(), body.ActorId);
        Assert.Contains(role, body.Roles);
    }

    [Fact]
    public async Task UnprivilegedUser_Receives403()
    {
        await using IdentitySessionBoundaryFixture fixture =
            await IdentitySessionBoundaryFixture.CreateAsync();
        var user = await fixture.CreateUserAsync();

        using HttpResponseMessage response = await fixture.SendForUserAsync(user.Id);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RoleRemoval_IsReflectedOnNextRequest()
    {
        await using IdentitySessionBoundaryFixture fixture =
            await IdentitySessionBoundaryFixture.CreateAsync();
        var user = await fixture.CreateUserAsync(IdentityRoleNames.WmsOperator);

        using HttpResponseMessage before = await fixture.SendForUserAsync(user.Id);
        await fixture.RemoveRoleAsync(user, IdentityRoleNames.WmsOperator);
        using HttpResponseMessage after = await fixture.SendForUserAsync(user.Id);

        Assert.Equal(HttpStatusCode.OK, before.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, after.StatusCode);
    }

    [Fact]
    public async Task AnonymousMissingIdAndUnknownUser_FailClosedWith401()
    {
        await using IdentitySessionBoundaryFixture fixture =
            await IdentitySessionBoundaryFixture.CreateAsync();

        using HttpResponseMessage anonymous = await fixture.SendAnonymousAsync();
        using HttpResponseMessage missingId = await fixture.SendMissingIdAsync();
        using HttpResponseMessage unknown = await fixture.SendForUserAsync(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, missingId.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
    }

    [Fact]
    public async Task ExpiredTamperedAndMismatchedTickets_Return401()
    {
        await using IdentitySessionBoundaryFixture fixture =
            await IdentitySessionBoundaryFixture.CreateAsync();
        DateTimeOffset expiredAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        string expired = fixture.ProtectTicket(
            issuedUtc: expiredAt.AddMinutes(-2),
            expiresUtc: expiredAt);
        string valid = fixture.ProtectTicket();
        string tampered = valid[..^1] + (valid[^1] == 'A' ? "B" : "A");
        string wrongKey = fixture.ProtectTicket(
            keyPath: fixture.CreateDifferentKeyPath());
        string wrongApplication = fixture.ProtectTicket(
            applicationName: "Different.Application");
        string wrongScheme = fixture.ProtectTicket(scheme: "Different.Scheme");

        foreach (string ticket in new[]
        {
            expired,
            tampered,
            wrongKey,
            wrongApplication,
            wrongScheme
        })
        {
            using HttpResponseMessage response = await fixture.SendRawTicketAsync(ticket);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [Fact]
    public async Task BrowserApplicationCookie_DoesNotAuthenticateApiService()
    {
        await using IdentitySessionBoundaryFixture fixture =
            await IdentitySessionBoundaryFixture.CreateAsync();
        string validApiSessionTicket = fixture.ProtectTicket();

        using HttpResponseMessage response = await fixture.SendBrowserCookieAsync(validApiSessionTicket);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
