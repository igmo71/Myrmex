using Microsoft.AspNetCore.Components;
using System.Net;

namespace Myrmex.WebApp.Identity;

public sealed class ProtectedApiAuthorizationHandler(
    NavigationManager navigationManager)
    : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage response = await base.SendAsync(
            request,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            navigationManager.NavigateTo(CreateLoginUrl(), forceLoad: true);
        }
        else if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            navigationManager.NavigateTo(
                "/account/access-denied",
                forceLoad: true);
        }

        return response;
    }

    private string CreateLoginUrl()
    {
        string relativePath = navigationManager.ToBaseRelativePath(
            navigationManager.Uri);
        string returnUrl = string.IsNullOrWhiteSpace(relativePath)
            ? "/"
            : $"/{relativePath}";

        return $"/account/login?returnUrl={Uri.EscapeDataString(returnUrl)}";
    }
}
