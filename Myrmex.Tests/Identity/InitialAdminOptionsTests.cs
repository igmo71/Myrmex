using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Myrmex.Identity.Application.Bootstrap;

namespace Myrmex.Tests.Identity;

public sealed class InitialAdminOptionsTests
{
    [Fact]
    public void DisabledDefaults_AreAccepted()
    {
        InitialAdminOptionsValidator validator = CreateValidator(
            Environments.Production);

        ValidateOptionsResult result = validator.Validate(
            null,
            new InitialAdminOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void EnabledWithoutEmail_IsRejected()
    {
        InitialAdminOptionsValidator validator = CreateValidator(
            Environments.Development);

        ValidateOptionsResult result = validator.Validate(
            null,
            CreateEnabledOptions(email: ""));

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains("Email", StringComparison.Ordinal));
    }

    [Fact]
    public void EnabledWithoutPassword_IsRejected()
    {
        InitialAdminOptionsValidator validator = CreateValidator(
            Environments.Development);

        ValidateOptionsResult result = validator.Validate(
            null,
            CreateEnabledOptions(password: ""));

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains("Password", StringComparison.Ordinal));
    }

    [Fact]
    public void EnabledWithInvalidEmail_IsRejected()
    {
        InitialAdminOptionsValidator validator = CreateValidator(
            Environments.Development);

        ValidateOptionsResult result = validator.Validate(
            null,
            CreateEnabledOptions(email: "not-an-email"));

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains("valid email", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EnabledWithInvalidPassword_IsRejected()
    {
        InitialAdminOptionsValidator validator = CreateValidator(
            Environments.Development);

        ValidateOptionsResult result = validator.Validate(
            null,
            CreateEnabledOptions(password: "password"));

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains("Password", StringComparison.Ordinal));
    }

    [Fact]
    public void ProductionEnabledWithoutPassword_IsRejectedAsMissingSecret()
    {
        InitialAdminOptionsValidator validator = CreateValidator(
            Environments.Production);

        ValidateOptionsResult result = validator.Validate(
            null,
            CreateEnabledOptions(password: ""));

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains("secret", StringComparison.OrdinalIgnoreCase));
    }

    private static InitialAdminOptions CreateEnabledOptions(
        string email = "admin@example.com",
        string password = "Myrmex1!") =>
        new()
        {
            Enabled = true,
            Email = email,
            Password = password,
            DisplayName = "Initial Administrator"
        };

    private static InitialAdminOptionsValidator CreateValidator(
        string environmentName) =>
        new(
            new TestHostEnvironment(environmentName),
            Options.Create(new IdentityOptions()));

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "Myrmex.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } =
            new NullFileProvider();
    }
}
