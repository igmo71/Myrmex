using Microsoft.AspNetCore.Components.Authorization;
using Myrmex.AspNetCore.Security;
using Myrmex.Identity.Infrastructure.Sessions;

namespace Myrmex.WebApp.Identity;

public sealed class IdentityApiAuthenticationHandler(
    AuthenticationStateProvider authenticationStateProvider,
    IIdentityApiSessionTicketIssuer ticketIssuer)
    : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        request.Headers.Remove("Cookie");
        request.Headers.Authorization = null;

        AuthenticationState authenticationState =
            await authenticationStateProvider.GetAuthenticationStateAsync();
        cancellationToken.ThrowIfCancellationRequested();

        string? protectedTicket = await ticketIssuer.IssueAsync(
            authenticationState.User,
            cancellationToken);
        if (protectedTicket is not null)
        {
            request.Headers.TryAddWithoutValidation(
                "Cookie",
                $"{MyrmexAuthenticationSchemes.ApiSessionCookieName}={protectedTicket}");
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
