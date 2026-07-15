using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Myrmex.Integrations.Synchronization.Configuration;

namespace Myrmex.Integrations.Synchronization.Processing;

internal sealed class SynchronizationWorker(
    IServiceScopeFactory scopeFactory,
    SynchronizationWakeUp wakeUp,
    IOptions<SynchronizationOptions> options,
    TimeProvider timeProvider,
    ILogger<SynchronizationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Synchronization worker is starting.");

        try
        {
            await RunStartupPassAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                using CancellationTokenSource waitCancellation =
                    CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                Task<bool> wakeUpTask =
                    wakeUp.Reader.WaitToReadAsync(waitCancellation.Token).AsTask();
                Task pollingDelayTask = Task.Delay(
                    TimeSpan.FromSeconds(options.Value.PollingIntervalSeconds),
                    timeProvider,
                    waitCancellation.Token);

                Task completed = await Task.WhenAny(
                    wakeUpTask,
                    pollingDelayTask);
                await waitCancellation.CancelAsync();

                if (completed == wakeUpTask)
                {
                    if (await wakeUpTask)
                    {
                        await RunWakeUpSignalAsync(stoppingToken);
                    }

                    continue;
                }

                await RunPollingPassAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Synchronization worker is stopping.");
        }
        catch (Exception exception)
        {
            logger.LogCritical(
                exception,
                "Synchronization worker loop exited unexpectedly.");
            throw;
        }
    }

    internal Task<WorkerPassResult> RunStartupPassAsync(
        CancellationToken cancellationToken) =>
        RunRecoveryThenProcessBatchAsync(
            "startup",
            cancellationToken);

    internal Task<WorkerPassResult> RunPollingPassAsync(
        CancellationToken cancellationToken) =>
        RunRecoveryThenProcessBatchAsync(
            "polling",
            cancellationToken);

    internal async Task<int> RunWakeUpSignalAsync(
        CancellationToken cancellationToken)
    {
        int signals = DrainWakeUpSignals();
        logger.LogDebug(
            "Synchronization worker received {SignalCount} wake-up signals.",
            signals);

        return await RunWakeUpDrainAsync(cancellationToken);
    }

    internal async Task<int> RunWakeUpDrainAsync(
        CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        SynchronizationProcessor processor =
            scope.ServiceProvider.GetRequiredService<SynchronizationProcessor>();

        int processed =
            await processor.ProcessEligibleUntilDrainedAsync(cancellationToken);

        logger.LogInformation(
            "Synchronization worker wake-up drain processed {ProcessedCount} eligible requests.",
            processed);

        return processed;
    }

    private async Task<WorkerPassResult> RunRecoveryThenProcessBatchAsync(
        string passName,
        CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        SynchronizationRequestStore store =
            scope.ServiceProvider.GetRequiredService<SynchronizationRequestStore>();
        SynchronizationProcessor processor =
            scope.ServiceProvider.GetRequiredService<SynchronizationProcessor>();
        SynchronizationRetryPolicy retryPolicy =
            scope.ServiceProvider.GetRequiredService<SynchronizationRetryPolicy>();
        SynchronizationOptions currentOptions = options.Value;

        logger.LogDebug(
            "Synchronization worker {PassName} pass is starting recovery.",
            passName);

        int recovered = await store.RecoverAbandonedProcessingAsync(
            TimeSpan.FromSeconds(currentOptions.ProcessingTimeoutSeconds),
            timeProvider.GetUtcNow(),
            retryPolicy,
            currentOptions,
            cancellationToken);

        int processed =
            await processor.ProcessEligibleBatchAsync(cancellationToken);

        logger.LogInformation(
            "Synchronization worker {PassName} pass recovered {RecoveredCount} abandoned requests and processed {ProcessedCount} eligible requests.",
            passName,
            recovered,
            processed);

        return new WorkerPassResult(recovered, processed);
    }

    private int DrainWakeUpSignals()
    {
        int signals = 0;
        while (wakeUp.Reader.TryRead(out _))
        {
            signals++;
        }

        return signals;
    }

    internal sealed record WorkerPassResult(
        int RecoveredCount,
        int ProcessedCount);
}
