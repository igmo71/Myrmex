using Myrmex.Integrations.Persistence;
using Myrmex.Integrations.Synchronization;
using Myrmex.Integrations.Synchronization.Processing;
using static Myrmex.Tests.Integrations.OneC.Synchronization.IntegrationSynchronizationProcessorTestSupport;

namespace Myrmex.Tests.Integrations.OneC.Synchronization;

public sealed class IntegrationSynchronizationWakeUpTests
{
    [Fact]
    public void WakeUpSignal_CoalescesWithoutRequestPayload()
    {
        SynchronizationWakeUp wakeUp = new();

        wakeUp.Notify();
        wakeUp.Notify();

        Assert.True(wakeUp.Reader.TryRead(out bool signal));
        Assert.True(signal);
        Assert.False(wakeUp.Reader.TryRead(out _));
    }

    [Fact]
    public async Task ProcessEligibleUntilDrained_ProcessesSqlBatchesUntilNoneEligible()
    {
        await using IntegrationSynchronizationSqlTestHost host =
            await IntegrationSynchronizationSqlTestHost.CreateAsync();
        await using IntegrationDbContext dbContext = host.CreateDbContext();
        SynchronizationRequest first = CreateRequest(
            receivedAtUtc: NowUtc.AddSeconds(-3));
        SynchronizationRequest second = CreateRequest(
            receivedAtUtc: NowUtc.AddSeconds(-2));
        SynchronizationRequest third = CreateRequest(
            receivedAtUtc: NowUtc.AddSeconds(-1));
        SynchronizationRequest future = CreateRequest(
            nextAttemptAtUtc: NowUtc.AddMinutes(10));
        await SeedAsync(dbContext, first, second, third, future);
        TestSynchronizationHandler handler = new((_, _) =>
            Task.FromResult(SynchronizationHandlerResult.Completed()));
        SynchronizationProcessor processor = CreateProcessor(
            dbContext,
            new MutableTimeProvider(NowUtc),
            CreateOptions(batchSize: 2, retryDelaysSeconds: [10]),
            handler);

        int processed = await processor.ProcessEligibleUntilDrainedAsync(
            TestContext.Current.CancellationToken);

        List<SynchronizationRequest> saved = await ReadAllAsync(host);
        Assert.Equal(3, processed);
        Assert.Equal(3, handler.CallCount);
        Assert.Equal(
            3,
            saved.Count(request => request.Status == SynchronizationStatus.Completed));
        Assert.Contains(
            saved,
            request =>
                request.Id == future.Id &&
                request.Status == SynchronizationStatus.Pending);
    }
}
