using Microsoft.Extensions.Options;

namespace Myrmex.Integrations.Synchronization;

internal sealed class IntegrationSynchronizationOptions
{
    public const string SectionName = "Myrmex:Integrations:Synchronization";

    public int PollingIntervalSeconds { get; set; } = 60;

    public int BatchSize { get; set; } = 20;

    public int ProcessingAttemptTimeoutSeconds { get; set; } = 30;

    public int ProcessingTimeoutSeconds { get; set; } = 300;

    public List<int> RetryDelaysSeconds { get; set; } =
        [10, 30, 120, 600, 1800, 3600, 10800];
}

internal sealed class IntegrationSynchronizationOptionsValidator
    : IValidateOptions<IntegrationSynchronizationOptions>
{
    public ValidateOptionsResult Validate(
        string? name,
        IntegrationSynchronizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string> failures = [];

        AddPositiveFailure(
            failures,
            nameof(IntegrationSynchronizationOptions.PollingIntervalSeconds),
            options.PollingIntervalSeconds);
        AddPositiveFailure(
            failures,
            nameof(IntegrationSynchronizationOptions.BatchSize),
            options.BatchSize);
        AddPositiveFailure(
            failures,
            nameof(IntegrationSynchronizationOptions.ProcessingAttemptTimeoutSeconds),
            options.ProcessingAttemptTimeoutSeconds);
        AddPositiveFailure(
            failures,
            nameof(IntegrationSynchronizationOptions.ProcessingTimeoutSeconds),
            options.ProcessingTimeoutSeconds);

        if (options.RetryDelaysSeconds is null)
        {
            failures.Add(
                $"{IntegrationSynchronizationOptions.SectionName}:" +
                $"{nameof(IntegrationSynchronizationOptions.RetryDelaysSeconds)} " +
                "must be configured as a collection.");
        }
        else
        {
            for (int index = 0; index < options.RetryDelaysSeconds.Count; index++)
            {
                if (options.RetryDelaysSeconds[index] <= 0)
                {
                    failures.Add(
                        $"{IntegrationSynchronizationOptions.SectionName}:" +
                        $"{nameof(IntegrationSynchronizationOptions.RetryDelaysSeconds)}[{index}] " +
                        "must be positive.");
                }
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void AddPositiveFailure(
        List<string> failures,
        string propertyName,
        int value)
    {
        if (value <= 0)
        {
            failures.Add(
                $"{IntegrationSynchronizationOptions.SectionName}:{propertyName} " +
                "must be positive.");
        }
    }
}
