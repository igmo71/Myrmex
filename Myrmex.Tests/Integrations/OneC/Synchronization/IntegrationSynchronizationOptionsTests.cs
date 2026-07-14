using Microsoft.Extensions.Options;
using Myrmex.Integrations.OneC.Configuration;
using Myrmex.Integrations.Synchronization;

namespace Myrmex.Tests.Integrations.OneC.Synchronization;

public sealed class IntegrationSynchronizationOptionsTests
{
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ApiKeyOptions_RejectMissingApiKey(string? apiKey)
    {
        OneCIntegrationApiKeyOptions options = CreateApiKeyOptions();
        options.ApiKey = apiKey;

        ValidateOptionsResult result = new OneCIntegrationApiKeyOptionsValidator()
            .Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains("ApiKey", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ApiKeyOptions_RejectEmptySourceSystem(string? sourceSystem)
    {
        OneCIntegrationApiKeyOptions options = CreateApiKeyOptions();
        options.SourceSystem = sourceSystem!;

        ValidateOptionsResult result = new OneCIntegrationApiKeyOptionsValidator()
            .Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains("SourceSystem", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ApiKeyOptions_RejectEmptySourceInstance(string? sourceInstance)
    {
        OneCIntegrationApiKeyOptions options = CreateApiKeyOptions();
        options.SourceInstance = sourceInstance;

        ValidateOptionsResult result = new OneCIntegrationApiKeyOptionsValidator()
            .Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains("SourceInstance", StringComparison.Ordinal));
    }

    [Fact]
    public void ApiKeyOptions_RejectOverLengthSourceSystem()
    {
        OneCIntegrationApiKeyOptions options = CreateApiKeyOptions();
        options.SourceSystem = new string(
            'S',
            OneCIntegrationApiKeyOptions.SourceSystemMaxLength + 1);

        ValidateOptionsResult result = new OneCIntegrationApiKeyOptionsValidator()
            .Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains("SourceSystem", StringComparison.Ordinal));
    }

    [Fact]
    public void ApiKeyOptions_RejectOverLengthSourceInstance()
    {
        OneCIntegrationApiKeyOptions options = CreateApiKeyOptions();
        options.SourceInstance = new string(
            'I',
            OneCIntegrationApiKeyOptions.SourceInstanceMaxLength + 1);

        ValidateOptionsResult result = new OneCIntegrationApiKeyOptionsValidator()
            .Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains("SourceInstance", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(nameof(IntegrationSynchronizationOptions.PollingIntervalSeconds))]
    [InlineData(nameof(IntegrationSynchronizationOptions.BatchSize))]
    [InlineData(nameof(IntegrationSynchronizationOptions.ProcessingAttemptTimeoutSeconds))]
    [InlineData(nameof(IntegrationSynchronizationOptions.ProcessingTimeoutSeconds))]
    public void SynchronizationOptions_RejectNonPositiveScalarValues(
        string propertyName)
    {
        IntegrationSynchronizationOptions options = CreateSynchronizationOptions();
        typeof(IntegrationSynchronizationOptions)
            .GetProperty(propertyName)!
            .SetValue(options, 0);

        ValidateOptionsResult result =
            new IntegrationSynchronizationOptionsValidator()
                .Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains(propertyName, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SynchronizationOptions_RejectNonPositiveRetryDelayElements(
        int delay)
    {
        IntegrationSynchronizationOptions options = CreateSynchronizationOptions();
        options.RetryDelaysSeconds = [10, delay];

        ValidateOptionsResult result =
            new IntegrationSynchronizationOptionsValidator()
                .Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains("RetryDelaysSeconds[1]", StringComparison.Ordinal));
    }

    [Fact]
    public void SynchronizationOptions_AcceptEmptyRetryDelays()
    {
        IntegrationSynchronizationOptions options = CreateSynchronizationOptions();
        options.RetryDelaysSeconds = [];

        ValidateOptionsResult result =
            new IntegrationSynchronizationOptionsValidator()
                .Validate(null, options);

        Assert.True(result.Succeeded);
    }

    private static OneCIntegrationApiKeyOptions CreateApiKeyOptions() =>
        new()
        {
            SourceSystem = OneCIntegrationApiKeyOptions.DefaultSourceSystem,
            SourceInstance = "warehouse-main",
            ApiKey = "development-only-key"
        };

    private static IntegrationSynchronizationOptions CreateSynchronizationOptions() =>
        new()
        {
            PollingIntervalSeconds = 60,
            BatchSize = 20,
            ProcessingAttemptTimeoutSeconds = 30,
            ProcessingTimeoutSeconds = 300,
            RetryDelaysSeconds = [10, 30]
        };
}
