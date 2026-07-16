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

        X509Certificate2? certificate = LoadCertificate(options.DataProtection.Certificate);
        if (certificate is not null)
        {
            dataProtection.ProtectKeysWithCertificate(certificate);
        }

        return services;
    }

    private static X509Certificate2? LoadCertificate(
    IdentityDataProtectionCertificateOptions options)
    {
        string? filePath = options.FilePath?.Trim();

        if (!string.IsNullOrWhiteSpace(filePath))
        {
            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException(
                    $"The configured Data Protection certificate file '{filePath}' was not found.");
            }

            X509Certificate2 certificate =
                X509CertificateLoader.LoadPkcs12FromFile(
                    filePath,
                    options.Password,
                    X509KeyStorageFlags.EphemeralKeySet);

            if (!certificate.HasPrivateKey)
            {
                certificate.Dispose();

                throw new InvalidOperationException(
                    "The configured Data Protection certificate does not contain a private key.");
            }

            return certificate;
        }

        string? thumbprint = options.Thumbprint?.Trim();

        if (string.IsNullOrWhiteSpace(thumbprint))
        {
            return null;
        }

        using X509Store store = new(options.StoreName, options.StoreLocation);
        store.Open(OpenFlags.ReadOnly);

        return store.Certificates
            .Find(
                X509FindType.FindByThumbprint,
                thumbprint,
                validOnly: false)
            .OfType<X509Certificate2>()
            .FirstOrDefault(candidate => candidate.HasPrivateKey)
            ?? throw new InvalidOperationException(
                "The configured Data Protection certificate was not found or does not " +
                "contain a private key.");
    }
}
