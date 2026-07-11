using Microsoft.Extensions.Configuration;
using System.Security.Cryptography.X509Certificates;

namespace Myrmex.Identity.Infrastructure.Configuration;

public sealed class IdentityDataProtectionOptions
{
    public const string SectionName = "Myrmex:Identity";

    public const int RequiredApiSessionLifetimeMinutes = 2;

    public IdentitySharedDataProtectionOptions DataProtection { get; set; } = new();

    public IdentityApiSessionOptions ApiSession { get; set; } = new();

    internal static IdentityDataProtectionOptions FromConfiguration(
        IConfiguration configuration)
    {
        IdentityDataProtectionOptions options = new();
        configuration.GetSection(SectionName).Bind(options);
        return options;
    }
}

public sealed class IdentitySharedDataProtectionOptions
{
    public string ApplicationName { get; set; } = "Myrmex";

    public bool AllowUnprotectedKeysInDevelopment { get; set; }

    public IdentityDataProtectionCertificateOptions Certificate { get; set; } = new();
}

public sealed class IdentityDataProtectionCertificateOptions
{
    public string? Thumbprint { get; set; }

    public StoreName StoreName { get; set; } =
        System.Security.Cryptography.X509Certificates.StoreName.My;

    public StoreLocation StoreLocation { get; set; } =
        System.Security.Cryptography.X509Certificates.StoreLocation.CurrentUser;
}

public sealed class IdentityApiSessionOptions
{
    public int LifetimeMinutes { get; set; } =
        IdentityDataProtectionOptions.RequiredApiSessionLifetimeMinutes;
}
