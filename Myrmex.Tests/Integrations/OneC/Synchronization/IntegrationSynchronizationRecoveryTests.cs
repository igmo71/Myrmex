using Myrmex.Integrations.Persistence;
using Myrmex.Integrations.Synchronization;
using Myrmex.Integrations.Synchronization.Processing;
using static Myrmex.Tests.Integrations.OneC.Synchronization.IntegrationSynchronizationProcessorTestSupport;

namespace Myrmex.Tests.Integrations.OneC.Synchronization;

public sealed class IntegrationSynchronizationRecoveryTests
{
    [Fact]
    public async Task RecoverAbandonedProcessing_WhenRetriesRemain_RequeuesImmediately()
    {
        await using IntegrationSynchronizationSqlTestHost host =
            await IntegrationSynchronizationSqlTestHost.CreateAsync();
        await using IntegrationDbContext dbContext = host.CreateDbContext();
        SynchronizationRequest request = CreateAbandonedProcessingRequest(
            attemptCount: 1);
        await SeedAsync(dbContext, request);
        SynchronizationRequestStore store = CreateStore(dbContext);

        int recovered = await store.RecoverAbandonedProcessingAsync(
            TimeSpan.FromSeconds(300),
            NowUtc,
            new SynchronizationRetryPolicy(),
            CreateOptions(retryDelaysSeconds: [10]),
            TestContext.Current.CancellationToken);

        SynchronizationRequest saved = await ReadAsync(host, request.Id);
        Assert.Equal(1, recovered);
        Assert.Equal(SynchronizationStatus.Pending, saved.Status);
        Assert.Equal(1, saved.AttemptCount);
        Assert.Null(saved.ProcessingStartedAtUtc);
        Assert.Null(saved.NextAttemptAtUtc);
        Assert.True(
            saved.LastError is
            { Length: <= SynchronizationRequest.LastErrorMaxLength });
    }

    [Fact]
    public async Task RecoverAbandonedProcessing_WhenRetriesExhausted_MarksFailed()
    {
        await using IntegrationSynchronizationSqlTestHost host =
            await IntegrationSynchronizationSqlTestHost.CreateAsync();
        await using IntegrationDbContext dbContext = host.CreateDbContext();
        SynchronizationRequest request = CreateAbandonedProcessingRequest(
            attemptCount: 1);
        await SeedAsync(dbContext, request);
        SynchronizationRequestStore store = CreateStore(dbContext);

        int recovered = await store.RecoverAbandonedProcessingAsync(
            TimeSpan.FromSeconds(300),
            NowUtc,
            new SynchronizationRetryPolicy(),
            CreateOptions(),
            TestContext.Current.CancellationToken);

        SynchronizationRequest saved = await ReadAsync(host, request.Id);
        Assert.Equal(1, recovered);
        Assert.Equal(SynchronizationStatus.Failed, saved.Status);
        Assert.Equal(1, saved.AttemptCount);
        Assert.Equal(NowUtc.AddSeconds(-301), saved.ProcessingStartedAtUtc);
        Assert.Null(saved.NextAttemptAtUtc);
        Assert.True(
            saved.LastError is
            { Length: <= SynchronizationRequest.LastErrorMaxLength });
    }

    [Fact]
    public async Task RecoverAbandonedProcessing_WhenProcessingIsFresh_DoesNothing()
    {
        await using IntegrationSynchronizationSqlTestHost host =
            await IntegrationSynchronizationSqlTestHost.CreateAsync();
        await using IntegrationDbContext dbContext = host.CreateDbContext();
        SynchronizationRequest request = CreateAbandonedProcessingRequest(
            attemptCount: 1,
            processingStartedAtUtc: NowUtc.AddSeconds(-299));
        await SeedAsync(dbContext, request);
        SynchronizationRequestStore store = CreateStore(dbContext);

        int recovered = await store.RecoverAbandonedProcessingAsync(
            TimeSpan.FromSeconds(300),
            NowUtc,
            new SynchronizationRetryPolicy(),
            CreateOptions(retryDelaysSeconds: [10]),
            TestContext.Current.CancellationToken);

        SynchronizationRequest saved = await ReadAsync(host, request.Id);
        Assert.Equal(0, recovered);
        Assert.Equal(SynchronizationStatus.Processing, saved.Status);
        Assert.Equal(1, saved.AttemptCount);
        Assert.Equal(NowUtc.AddSeconds(-299), saved.ProcessingStartedAtUtc);
        Assert.Null(saved.LastError);
    }

    private static SynchronizationRequest CreateAbandonedProcessingRequest(
        int attemptCount,
        DateTimeOffset? processingStartedAtUtc = null)
    {
        SynchronizationRequest request = CreateRequest();
        request.Status = SynchronizationStatus.Processing;
        request.AttemptCount = attemptCount;
        request.ProcessingStartedAtUtc =
            processingStartedAtUtc ?? NowUtc.AddSeconds(-301);
        return request;
    }
}
