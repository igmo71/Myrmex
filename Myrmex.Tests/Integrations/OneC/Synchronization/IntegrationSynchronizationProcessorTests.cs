using Myrmex.Integrations.Persistence;
using Myrmex.Integrations.Synchronization;
using Myrmex.Integrations.Synchronization.Processing;
using static Myrmex.Tests.Integrations.OneC.Synchronization.IntegrationSynchronizationProcessorTestSupport;

namespace Myrmex.Tests.Integrations.OneC.Synchronization;

public sealed class IntegrationSynchronizationProcessorTests
{
    [Fact]
    public async Task ProcessEligibleBatch_WhenHandlerUnsupported_DefersWithoutAttempt()
    {
        await using IntegrationSynchronizationSqlTestHost host =
            await IntegrationSynchronizationSqlTestHost.CreateAsync();
        await using IntegrationDbContext dbContext = host.CreateDbContext();
        SynchronizationRequest request = CreateRequest();
        await SeedAsync(dbContext, request);
        MutableTimeProvider timeProvider = new(NowUtc);
        SynchronizationProcessor processor = CreateProcessor(
            dbContext,
            timeProvider,
            CreateOptions(retryDelaysSeconds: [10]));

        int processed = await processor.ProcessEligibleBatchAsync(
            TestContext.Current.CancellationToken);

        SynchronizationRequest saved = await ReadAsync(host, request.Id);
        Assert.Equal(1, processed);
        Assert.Equal(SynchronizationStatus.Deferred, saved.Status);
        Assert.Equal(0, saved.AttemptCount);
        Assert.Null(saved.ProcessingStartedAtUtc);
        Assert.Null(saved.NextAttemptAtUtc);
    }

    [Fact]
    public async Task ProcessEligibleBatch_CommitsProcessingAttemptBeforeHandler()
    {
        await using IntegrationSynchronizationSqlTestHost host =
            await IntegrationSynchronizationSqlTestHost.CreateAsync();
        await using IntegrationDbContext dbContext = host.CreateDbContext();
        SynchronizationRequest request = CreateRequest();
        await SeedAsync(dbContext, request);
        MutableTimeProvider timeProvider = new(NowUtc);
        bool observedCommittedProcessing = false;
        TestSynchronizationHandler handler = new(
            async (processingRequest, cancellationToken) =>
            {
                SynchronizationRequest committed =
                    await ReadAsync(host, processingRequest.Id);
                observedCommittedProcessing =
                    committed.Status == SynchronizationStatus.Processing &&
                    committed.AttemptCount == 1 &&
                    committed.ProcessingStartedAtUtc == NowUtc;

                return SynchronizationHandlerResult.Completed();
            });
        SynchronizationProcessor processor = CreateProcessor(
            dbContext,
            timeProvider,
            CreateOptions(retryDelaysSeconds: [10]),
            handler);

        int processed = await processor.ProcessEligibleBatchAsync(
            TestContext.Current.CancellationToken);

        SynchronizationRequest saved = await ReadAsync(host, request.Id);
        Assert.Equal(1, processed);
        Assert.True(observedCommittedProcessing);
        Assert.Equal(1, handler.CallCount);
        Assert.Equal(SynchronizationStatus.Completed, saved.Status);
        Assert.Equal(1, saved.AttemptCount);
        Assert.Equal(NowUtc, saved.ProcessingStartedAtUtc);
        Assert.Equal(NowUtc, saved.CompletedAtUtc);
        Assert.Null(saved.NextAttemptAtUtc);
    }
}
