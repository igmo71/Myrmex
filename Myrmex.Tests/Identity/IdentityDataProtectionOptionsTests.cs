using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Myrmex.Identity.Infrastructure.Configuration;

namespace Myrmex.Tests.Identity;

public sealed class IdentityDataProtectionOptionsTests
{
    [Fact]
    public void ProductionWithoutCertificateProtection_IsRejected()
    {
        IdentityDataProtectionOptions options = CreateValidOptions();
        options.DataProtection.Certificate.Thumbprint = null;

        IdentityDataProtectionOptionsValidator validator = new(
            new TestHostEnvironment(Environments.Production));

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains("certificate", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ProductionWithDevelopmentOptOut_IsRejected()
    {
        IdentityDataProtectionOptions options = CreateValidOptions();
        options.DataProtection.AllowUnprotectedKeysInDevelopment = true;

        IdentityDataProtectionOptionsValidator validator = new(
            new TestHostEnvironment(Environments.Production));

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
    }

    [Fact]
    public void DevelopmentWithExplicitUnprotectedKeyConfiguration_IsAccepted()
    {
        IdentityDataProtectionOptions options = CreateValidOptions();
        options.DataProtection.Certificate.Thumbprint = null;
        options.DataProtection.AllowUnprotectedKeysInDevelopment = true;

        IdentityDataProtectionOptionsValidator validator = new(
            new TestHostEnvironment(Environments.Development));

        var result = validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void DevelopmentWithoutCertificateOrExplicitOptOut_IsRejected()
    {
        IdentityDataProtectionOptions options = CreateValidOptions();
        options.DataProtection.Certificate.Thumbprint = null;
        options.DataProtection.AllowUnprotectedKeysInDevelopment = false;

        IdentityDataProtectionOptionsValidator validator = new(
            new TestHostEnvironment(Environments.Development));

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
    }

    private static IdentityDataProtectionOptions CreateValidOptions()
    {
        return new IdentityDataProtectionOptions
        {
            ApiSession = new IdentityApiSessionOptions
            {
                LifetimeMinutes = IdentityDataProtectionOptions.RequiredApiSessionLifetimeMinutes
            },
            DataProtection = new IdentitySharedDataProtectionOptions
            {
                ApplicationName = "Myrmex",
                Certificate = new IdentityDataProtectionCertificateOptions
                {
                    Thumbprint = "00112233445566778899AABBCCDDEEFF00112233"
                }
            }
        };
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "Myrmex.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
