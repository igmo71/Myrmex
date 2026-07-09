using Microsoft.AspNetCore.Components.Authorization;
using Myrmex.AspNetCore.Security;
using Myrmex.Identity.Infrastructure.Sessions;
using Myrmex.WebApp.Identity;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace Myrmex.Tests.Identity;

public sealed class IdentityApiAuthenticationHandlerTests
{
    [Fact]
    public async Task SendAsync_UsesAuthenticationStateAndAttachesOnlyInternalCookie()
    {
        Guid userId = Guid.NewGuid();
        TestAuthenticationStateProvider authenticationState = new(
            CreatePrincipal(userId));
        RecordingTicketIssuer issuer = new("protected-api-ticket");
        RecordingHandler inner = new();
        using HttpClient client = CreateClient(authenticationState, issuer, inner);
        using HttpRequestMessage request = new(HttpMethod.Get, "/protected");
        request.Headers.TryAddWithoutValidation(
            "Cookie",
            ".Myrmex.Identity.Application=raw-browser-cookie; other=value");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "token");

        await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(1, authenticationState.CallCount);
        Assert.Same(authenticationState.Principal, issuer.Principal);
        Assert.Equal(
            $"{MyrmexAuthenticationSchemes.ApiSessionCookieName}=protected-api-ticket",
            Assert.Single(inner.Request!.Headers.GetValues("Cookie")));
        Assert.Null(inner.Request.Headers.Authorization);
        Assert.DoesNotContain(inner.Request.Headers, header =>
            header.Key.Contains("user", StringComparison.OrdinalIgnoreCase) ||
            header.Key.Contains("role", StringComparison.OrdinalIgnoreCase) ||
            header.Key.Contains("identity", StringComparison.OrdinalIgnoreCase) ||
            header.Key.Contains("actor", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task SendAsync_WithoutIssuableIdentity_DoesNotAttachCookie(
        bool authenticated,
        bool invalidId)
    {
        ClaimsPrincipal principal = authenticated
            ? new ClaimsPrincipal(new ClaimsIdentity(
                invalidId
                    ? [new Claim(ClaimTypes.NameIdentifier, "not-a-guid")]
                    : [],
                "Test"))
            : new ClaimsPrincipal(new ClaimsIdentity());
        TestAuthenticationStateProvider authenticationState = new(principal);
        RecordingTicketIssuer issuer = new(protectedTicket: null);
        RecordingHandler inner = new();
        using HttpClient client = CreateClient(authenticationState, issuer, inner);

        await client.GetAsync("/protected", TestContext.Current.CancellationToken);

        Assert.False(inner.Request!.Headers.Contains("Cookie"));
        Assert.Null(inner.Request.Headers.Authorization);
    }

    private static HttpClient CreateClient(
        AuthenticationStateProvider authenticationStateProvider,
        IIdentityApiSessionTicketIssuer issuer,
        HttpMessageHandler inner)
    {
        IdentityApiAuthenticationHandler handler = new(
            authenticationStateProvider,
            issuer)
        {
            InnerHandler = inner
        };
        return new HttpClient(handler) { BaseAddress = new Uri("https://api.test") };
    }

    private static ClaimsPrincipal CreatePrincipal(Guid userId) =>
        new(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
            "Identity.Application"));

    private sealed class TestAuthenticationStateProvider(ClaimsPrincipal principal)
        : AuthenticationStateProvider
    {
        public ClaimsPrincipal Principal { get; } = principal;

        public int CallCount { get; private set; }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            CallCount++;
            return Task.FromResult(new AuthenticationState(Principal));
        }
    }

    private sealed class RecordingTicketIssuer(string? protectedTicket)
        : IIdentityApiSessionTicketIssuer
    {
        public ClaimsPrincipal? Principal { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public Task<string?> IssueAsync(
            ClaimsPrincipal principal,
            CancellationToken cancellationToken = default)
        {
            Principal = principal;
            CancellationToken = cancellationToken;
            return Task.FromResult(protectedTicket);
        }
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Request = request;
            CancellationToken = cancellationToken;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
