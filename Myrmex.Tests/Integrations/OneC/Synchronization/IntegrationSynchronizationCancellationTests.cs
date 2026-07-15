using Myrmex.Integrations.Persistence;
using Myrmex.Integrations.Synchronization;
using Myrmex.Integrations.Synchronization.Processing;
using static Myrmex.Tests.Integrations.OneC.Synchronization.IntegrationSynchronizationProcessorTestSupport;

namespace Myrmex.Tests.Integrations.OneC.Synchronization;

public sealed class IntegrationSynchronizationCancellationTests
{
    [Fact]
    public async Task ProcessEligibleBatch_WhenHostShutdownCancelsHandler_LeavesRequestProcessing()
    {
        await using IntegrationSynchronizationSqlTestHost host =
            await IntegrationSynchronizationSqlTestHost.CreateAsync();
        await using IntegrationDbContext dbContext = host.CreateDbContext();
        SynchronizationRequest request = CreateRequest();
        await SeedAsync(dbContext, request);
        TaskCompletionSource handlerStarted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        TestSynchronizationHandler handler = new(async (_, cancellationToken) =>
        {
            handlerStarted.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return SynchronizationHandlerResult.Completed();
        });
        SynchronizationProcessor processor = CreateProcessor(
            dbContext,
            new MutableTimeProvider(NowUtc),
            CreateOptions(retryDelaysSeconds: [10]),
            handler);
        using CancellationTokenSource shutdown = new();

        Task processing = processor.ProcessEligibleBatchAsync(shutdown.Token);
        await handlerStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        await shutdown.CancelAsync();
        await processing;

        SynchronizationRequest saved = await ReadAsync(host, request.Id);
        Assert.Equal(SynchronizationStatus.Processing, saved.Status);
        Assert.Equal(1, saved.AttemptCount);
        Assert.Equal(NowUtc, saved.ProcessingStartedAtUtc);
        Assert.Null(saved.NextAttemptAtUtc);
        Assert.Null(saved.CompletedAtUtc);
        Assert.Null(saved.LastError);
    }
}
