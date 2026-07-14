using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Myrmex.AspNetCore.Security;
using Myrmex.Integrations.OneC.Configuration;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;

namespace Myrmex.Integrations.OneC.Security;

internal sealed class IntegrationApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    IOptionsMonitor<OneCIntegrationApiKeyOptions> apiKeyOptions,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private const string AuthorizationHeaderName = "Authorization";
    private const string AuthorizationPrefix = "ApiKey ";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(AuthorizationHeaderName, out var headerValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        string? header = headerValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(header) ||
            !header.StartsWith(AuthorizationPrefix, StringComparison.Ordinal))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid integration API key."));
        }

        string presentedKey = header[AuthorizationPrefix.Length..];
        string? configuredKey = apiKeyOptions.CurrentValue.ApiKey;
        if (string.IsNullOrWhiteSpace(configuredKey) ||
            !KeysMatch(presentedKey, configuredKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid integration API key."));
        }

        ClaimsIdentity identity = new(
            [
                new Claim(
                    ClaimTypes.Name,
                    MyrmexAuthenticationSchemes.IntegrationApiKey)
            ],
            Scheme.Name);
        ClaimsPrincipal principal = new(identity);
        AuthenticationTicket ticket = new(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static bool KeysMatch(
    string presentedKey,
    string configuredKey)
    {
        byte[] presentedHash = SHA256.HashData(Encoding.UTF8.GetBytes(presentedKey));
        byte[] configuredHash = SHA256.HashData(Encoding.UTF8.GetBytes(configuredKey));

        return CryptographicOperations.FixedTimeEquals(
            presentedHash,
            configuredHash);
    }
}
