using Myrmex.Integrations.Synchronization.Configuration;

namespace Myrmex.Integrations.Synchronization.Processing;

internal sealed class SynchronizationRetryPolicy
{
    public SynchronizationRetryDecision GetTransientFailureDecision(
        SynchronizationOptions options,
        int attemptCount,
        DateTimeOffset failedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (attemptCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(attemptCount),
                "Attempt count must be positive after a processing attempt starts.");
        }

        if (attemptCount > options.RetryDelaysSeconds.Count)
        {
            return SynchronizationRetryDecision.Fail();
        }

        int delaySeconds = options.RetryDelaysSeconds[attemptCount - 1];
        return SynchronizationRetryDecision.RetryAt(
            failedAtUtc.AddSeconds(delaySeconds));
    }
}

internal sealed record SynchronizationRetryDecision(
    bool ShouldRetry,
    DateTimeOffset? NextAttemptAtUtc)
{
    public static SynchronizationRetryDecision RetryAt(
        DateTimeOffset nextAttemptAtUtc) =>
        new(ShouldRetry: true, nextAttemptAtUtc);

    public static SynchronizationRetryDecision Fail() =>
        new(ShouldRetry: false, NextAttemptAtUtc: null);
}
