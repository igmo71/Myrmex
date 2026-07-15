using Myrmex.Integrations.Persistence;
using Myrmex.Integrations.Synchronization;
using Myrmex.Integrations.Synchronization.Configuration;
using Myrmex.Integrations.Synchronization.Processing;
using static Myrmex.Tests.Integrations.OneC.Synchronization.IntegrationSynchronizationProcessorTestSupport;

namespace Myrmex.Tests.Integrations.OneC.Synchronization;

public sealed class IntegrationSynchronizationRetryTests
{
    [Fact]
    public async Task ProcessEligibleBatch_WhenTransientFailureHasRetryDelay_RequeuesWithNextDelay()
    {
        await using IntegrationSynchronizationSqlTestHost host =
            await IntegrationSynchronizationSqlTestHost.CreateAsync();
        await using IntegrationDbContext dbContext = host.CreateDbContext();
        SynchronizationRequest request = CreateRequest();
        await SeedAsync(dbContext, request);
        MutableTimeProvider timeProvider = new(NowUtc);
        TestSynchronizationHandler handler = new((_, _) =>
            Task.FromResult(
                SynchronizationHandlerResult.TransientFailure(
                    "temporary source outage")));
        SynchronizationProcessor processor = CreateProcessor(
            dbContext,
            timeProvider,
            CreateOptions(retryDelaysSeconds: [10, 30]),
            handler);

        await processor.ProcessEligibleBatchAsync(
            TestContext.Current.CancellationToken);

        SynchronizationRequest saved = await ReadAsync(host, request.Id);
        Assert.Equal(SynchronizationStatus.Pending, saved.Status);
        Assert.Equal(1, saved.AttemptCount);
        Assert.Equal(NowUtc, saved.ProcessingStartedAtUtc);
        Assert.Equal(NowUtc.AddSeconds(10), saved.NextAttemptAtUtc);
        Assert.Equal("temporary source outage", saved.LastError);
    }

    [Fact]
    public void RetryPolicy_NDelaysPermitNPlusOneAttempts()
    {
        SynchronizationOptions options = CreateOptions(
            retryDelaysSeconds: [10, 30]);
        SynchronizationRetryPolicy policy = new();

        SynchronizationRetryDecision first =
            policy.GetTransientFailureDecision(options, 1, NowUtc);
        SynchronizationRetryDecision second =
            policy.GetTransientFailureDecision(options, 2, NowUtc);
        SynchronizationRetryDecision third =
            policy.GetTransientFailureDecision(options, 3, NowUtc);

        Assert.True(first.ShouldRetry);
        Assert.Equal(NowUtc.AddSeconds(10), first.NextAttemptAtUtc);
        Assert.True(second.ShouldRetry);
        Assert.Equal(NowUtc.AddSeconds(30), second.NextAttemptAtUtc);
        Assert.False(third.ShouldRetry);
        Assert.Null(third.NextAttemptAtUtc);
    }

    [Fact]
    public async Task ProcessEligibleBatch_WhenRetryDelaysEmpty_TransientFailureBecomesFailed()
    {
        await using IntegrationSynchronizationSqlTestHost host =
            await IntegrationSynchronizationSqlTestHost.CreateAsync();
        await using IntegrationDbContext dbContext = host.CreateDbContext();
        SynchronizationRequest request = CreateRequest();
        await SeedAsync(dbContext, request);
        TestSynchronizationHandler handler = new((_, _) =>
            Task.FromResult(
                SynchronizationHandlerResult.TransientFailure(
                    "temporary source outage")));
        SynchronizationProcessor processor = CreateProcessor(
            dbContext,
            new MutableTimeProvider(NowUtc),
            CreateOptions(),
            handler);

        await processor.ProcessEligibleBatchAsync(
            TestContext.Current.CancellationToken);

        SynchronizationRequest saved = await ReadAsync(host, request.Id);
        Assert.Equal(SynchronizationStatus.Failed, saved.Status);
        Assert.Equal(1, saved.AttemptCount);
        Assert.Equal(NowUtc, saved.ProcessingStartedAtUtc);
        Assert.Null(saved.NextAttemptAtUtc);
        Assert.Equal("temporary source outage", saved.LastError);
    }

    [Fact]
    public async Task ProcessEligibleBatch_WhenPermanentFailure_ReturnsTerminalFailed()
    {
        await using IntegrationSynchronizationSqlTestHost host =
            await IntegrationSynchronizationSqlTestHost.CreateAsync();
        await using IntegrationDbContext dbContext = host.CreateDbContext();
        SynchronizationRequest request = CreateRequest();
        await SeedAsync(dbContext, request);
        TestSynchronizationHandler handler = new((_, _) =>
            Task.FromResult(
                SynchronizationHandlerResult.PermanentFailure(
                    "document contract is unsupported")));
        SynchronizationProcessor processor = CreateProcessor(
            dbContext,
            new MutableTimeProvider(NowUtc),
            CreateOptions(retryDelaysSeconds: [10, 30]),
            handler);

        await processor.ProcessEligibleBatchAsync(
            TestContext.Current.CancellationToken);

        SynchronizationRequest saved = await ReadAsync(host, request.Id);
        Assert.Equal(SynchronizationStatus.Failed, saved.Status);
        Assert.Equal(1, saved.AttemptCount);
        Assert.Equal(NowUtc, saved.ProcessingStartedAtUtc);
        Assert.Null(saved.NextAttemptAtUtc);
        Assert.Equal("document contract is unsupported", saved.LastError);
    }

    [Fact]
    public async Task ProcessEligibleBatch_WhenProcessingAttemptTimesOut_TreatsAsTransientFailure()
    {
        await using IntegrationSynchronizationSqlTestHost host =
            await IntegrationSynchronizationSqlTestHost.CreateAsync();
        await using IntegrationDbContext dbContext = host.CreateDbContext();
        SynchronizationRequest request = CreateRequest();
        await SeedAsync(dbContext, request);
        TestSynchronizationHandler handler = new(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return SynchronizationHandlerResult.Completed();
        });
        SynchronizationProcessor processor = CreateProcessor(
            dbContext,
            new MutableTimeProvider(NowUtc),
            CreateOptions(processingAttemptTimeoutSeconds: 1),
            handler);

        await processor.ProcessEligibleBatchAsync(
            TestContext.Current.CancellationToken);

        SynchronizationRequest saved = await ReadAsync(host, request.Id);
        Assert.Equal(SynchronizationStatus.Failed, saved.Status);
        Assert.Equal(1, saved.AttemptCount);
        Assert.Equal(NowUtc, saved.ProcessingStartedAtUtc);
        Assert.Null(saved.NextAttemptAtUtc);
        Assert.Equal("Processing attempt timed out.", saved.LastError);
    }
}
