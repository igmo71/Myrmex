using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Myrmex.Identity.Infrastructure.Configuration;

public sealed class IdentityDataProtectionOptionsValidator(
    IHostEnvironment environment)
    : IValidateOptions<IdentityDataProtectionOptions>
{
    public ValidateOptionsResult Validate(
        string? name,
        IdentityDataProtectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string> failures = [];

        if (string.IsNullOrWhiteSpace(options.DataProtection.ApplicationName))
        {
            failures.Add(
                $"{IdentityDataProtectionOptions.SectionName}:DataProtection:ApplicationName " +
                "must be configured.");
        }

        if (options.ApiSession.LifetimeMinutes !=
            IdentityDataProtectionOptions.RequiredApiSessionLifetimeMinutes)
        {
            failures.Add(
                $"{IdentityDataProtectionOptions.SectionName}:ApiSession:LifetimeMinutes " +
                $"must be {IdentityDataProtectionOptions.RequiredApiSessionLifetimeMinutes}.");
        }

        bool hasCertificate = !string.IsNullOrWhiteSpace(
            options.DataProtection.Certificate.Thumbprint);
        bool allowsDevelopmentOptOut =
            options.DataProtection.AllowUnprotectedKeysInDevelopment;

        if (allowsDevelopmentOptOut && !environment.IsDevelopment())
        {
            failures.Add(
                $"{IdentityDataProtectionOptions.SectionName}:DataProtection:" +
                "AllowUnprotectedKeysInDevelopment is permitted only in Development.");
        }

        if (!hasCertificate &&
            !(environment.IsDevelopment() && allowsDevelopmentOptOut))
        {
            failures.Add(
                $"{IdentityDataProtectionOptions.SectionName}:DataProtection:Certificate:" +
                "Thumbprint must identify an X.509 certificate with a private key.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
