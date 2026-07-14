using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Myrmex.Identity.Infrastructure.Configuration;
using Myrmex.Identity.Persistence;
using System.Security.Cryptography.X509Certificates;

namespace Myrmex.Identity.Infrastructure;

public static class IdentityDataProtectionExtensions
{
    public static IServiceCollection AddMyrmexIdentityDataProtection(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        IConfigurationSection section = configuration.GetSection(
            IdentityDataProtectionOptions.SectionName);
        IdentityDataProtectionOptions options =
            IdentityDataProtectionOptions.FromConfiguration(configuration);
        IdentityDataProtectionOptionsValidator validator = new(environment);
        ValidateOptionsResult validation = validator.Validate(null, options);
        if (validation.Failed)
        {
            throw new OptionsValidationException(
                Options.DefaultName,
                typeof(IdentityDataProtectionOptions),
                validation.Failures);
        }

        services.AddOptions<IdentityDataProtectionOptions>()
            .Bind(section)
            .ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IValidateOptions<IdentityDataProtectionOptions>,
                IdentityDataProtectionOptionsValidator>());

        IDataProtectionBuilder dataProtection = services
            .AddDataProtection()
            .SetApplicationName(options.DataProtection.ApplicationName.Trim())
            .PersistKeysToDbContext<IdentityDbContext>();

        string? thumbprint = options.DataProtection.Certificate.Thumbprint?.Trim();
        if (!string.IsNullOrWhiteSpace(thumbprint))
        {
            dataProtection.ProtectKeysWithCertificate(
                LoadCertificate(options.DataProtection.Certificate, thumbprint));
        }

        return services;
    }

    private static X509Certificate2 LoadCertificate(
        IdentityDataProtectionCertificateOptions options,
        string thumbprint)
    {
        using X509Store store = new(options.StoreName, options.StoreLocation);
        store.Open(OpenFlags.ReadOnly);

        X509Certificate2? certificate = store.Certificates
            .Find(
                X509FindType.FindByThumbprint,
                thumbprint,
                validOnly: false)
            .OfType<X509Certificate2>()
            .FirstOrDefault(candidate => candidate.HasPrivateKey);

        return certificate ?? throw new InvalidOperationException(
            "The configured Data Protection certificate was not found or does not " +
            "contain a private key.");
    }
}
