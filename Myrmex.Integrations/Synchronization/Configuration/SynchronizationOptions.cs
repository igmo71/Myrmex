using Microsoft.Extensions.Options;

namespace Myrmex.Integrations.Synchronization.Configuration;

internal sealed class SynchronizationOptions
{
    public const string SectionName = "Myrmex:Integrations:Synchronization";

    public int PollingIntervalSeconds { get; set; } = 60;

    public int BatchSize { get; set; } = 20;

    public int ProcessingAttemptTimeoutSeconds { get; set; } = 30;

    public int ProcessingTimeoutSeconds { get; set; } = 300;

    public List<int> RetryDelaysSeconds { get; set; } =
        [10, 30, 120, 600, 1800, 3600, 10800];
}

internal sealed class SynchronizationOptionsValidator
    : IValidateOptions<SynchronizationOptions>
{
    public ValidateOptionsResult Validate(
        string? name,
        SynchronizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string> failures = [];

        AddPositiveFailure(
            failures,
            nameof(SynchronizationOptions.PollingIntervalSeconds),
            options.PollingIntervalSeconds);
        AddPositiveFailure(
            failures,
            nameof(SynchronizationOptions.BatchSize),
            options.BatchSize);
        AddPositiveFailure(
            failures,
            nameof(SynchronizationOptions.ProcessingAttemptTimeoutSeconds),
            options.ProcessingAttemptTimeoutSeconds);
        AddPositiveFailure(
            failures,
            nameof(SynchronizationOptions.ProcessingTimeoutSeconds),
            options.ProcessingTimeoutSeconds);

        if (options.RetryDelaysSeconds is null)
        {
            failures.Add(
                $"{SynchronizationOptions.SectionName}:" +
                $"{nameof(SynchronizationOptions.RetryDelaysSeconds)} " +
                "must be configured as a collection.");
        }
        else
        {
            for (int index = 0; index < options.RetryDelaysSeconds.Count; index++)
            {
                if (options.RetryDelaysSeconds[index] <= 0)
                {
                    failures.Add(
                        $"{SynchronizationOptions.SectionName}:" +
                        $"{nameof(SynchronizationOptions.RetryDelaysSeconds)}[{index}] " +
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
                $"{SynchronizationOptions.SectionName}:{propertyName} " +
                "must be positive.");
        }
    }
}
