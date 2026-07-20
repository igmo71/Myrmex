using Myrmex.Integrations.Persistence;
using Myrmex.Integrations.Synchronization;
using Myrmex.Integrations.Synchronization.Configuration;
using Myrmex.Integrations.Synchronization.Processing;
using static Myrmex.Tests.Integrations.OneC.Synchronization.IntegrationSynchronizationProcessorTestSupport;

namespace Myrmex.Tests.Integrations.OneC.Synchronization;

public sealed class IntegrationSynchronizationWorkerTests
{
    [Fact]
    public async Task RunStartupPass_RecoversAbandonedProcessingBeforeProcessingEligibleWork()
    {
        await using IntegrationSynchronizationSqlTestHost host =
            await IntegrationSynchronizationSqlTestHost.CreateAsync();
        await using (IntegrationDbContext dbContext = host.CreateDbContext())
        {
            SynchronizationRequest abandoned = CreateProcessingRequest(
                attemptCount: 1,
                processingStartedAtUtc: NowUtc.AddSeconds(-301));
            await SeedAsync(dbContext, abandoned);
        }

        MutableTimeProvider timeProvider = new(NowUtc);
        SynchronizationOptions options =
            CreateOptions(processingTimeoutSeconds: 300, retryDelaysSeconds: [10]);
        SynchronizationWakeUp wakeUp = new();
        TestSynchronizationHandler handler = new((_, _) =>
            Task.FromResult(SynchronizationHandlerResult.Completed()));
        await using ServiceProvider provider = CreateWorkerServiceProvider(
            host.ConnectionString,
            timeProvider,
            options,
            wakeUp,
            handler);
        SynchronizationWorker worker = CreateWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            wakeUp,
            timeProvider,
            options);

        SynchronizationWorker.WorkerPassResult result =
            await worker.RunStartupPassAsync(TestContext.Current.CancellationToken);

        SynchronizationRequest saved =
            (await ReadAllAsync(host)).Single();
        Assert.Equal(1, result.RecoveredCount);
        Assert.Equal(1, result.ProcessedCount);
        Assert.Equal(SynchronizationStatus.Completed, saved.Status);
        Assert.Equal(2, saved.AttemptCount);
        Assert.Equal(NowUtc, saved.ProcessingStartedAtUtc);
        Assert.Equal(NowUtc, saved.CompletedAtUtc);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task RunPollingPass_RecoversBeforeProcessingEligibleBatch()
    {
        await using IntegrationSynchronizationSqlTestHost host =
            await IntegrationSynchronizationSqlTestHost.CreateAsync();
        SynchronizationRequest abandoned = CreateProcessingRequest(
            attemptCount: 1,
            processingStartedAtUtc: NowUtc.AddSeconds(-301));
        SynchronizationRequest pending = CreateRequest(
            receivedAtUtc: NowUtc.AddSeconds(-1));
        await using (IntegrationDbContext dbContext = host.CreateDbContext())
        {
            await SeedAsync(dbContext, abandoned, pending);
        }

        MutableTimeProvider timeProvider = new(NowUtc);
        SynchronizationOptions options =
            CreateOptions(processingTimeoutSeconds: 300);
        SynchronizationWakeUp wakeUp = new();
        TestSynchronizationHandler handler = new((_, _) =>
            Task.FromResult(SynchronizationHandlerResult.Completed()));
        await using ServiceProvider provider = CreateWorkerServiceProvider(
            host.ConnectionString,
            timeProvider,
            options,
            wakeUp,
            handler);
        SynchronizationWorker worker = CreateWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            wakeUp,
            timeProvider,
            options);

        SynchronizationWorker.WorkerPassResult result =
            await worker.RunPollingPassAsync(TestContext.Current.CancellationToken);

        List<SynchronizationRequest> saved = await ReadAllAsync(host);
        Assert.Equal(1, result.RecoveredCount);
        Assert.Equal(1, result.ProcessedCount);
        Assert.Contains(
            saved,
            request =>
                request.Id == abandoned.Id &&
                request.Status == SynchronizationStatus.Failed &&
                request.AttemptCount == 1);
        Assert.Contains(
            saved,
            request =>
                request.Id == pending.Id &&
                request.Status == SynchronizationStatus.Completed &&
                request.AttemptCount == 1);
    }

    [Fact]
    public async Task RunWakeUpSignal_DrainsCoalescedSignalsUntilNoEligibleWorkRemains()
    {
        await using IntegrationSynchronizationSqlTestHost host =
            await IntegrationSynchronizationSqlTestHost.CreateAsync();
        SynchronizationRequest first = CreateRequest(
            receivedAtUtc: NowUtc.AddSeconds(-3));
        SynchronizationRequest second = CreateRequest(
            receivedAtUtc: NowUtc.AddSeconds(-2));
        SynchronizationRequest third = CreateRequest(
            receivedAtUtc: NowUtc.AddSeconds(-1));
        await using (IntegrationDbContext dbContext = host.CreateDbContext())
        {
            await SeedAsync(dbContext, first, second, third);
        }

        MutableTimeProvider timeProvider = new(NowUtc);
        SynchronizationOptions options =
            CreateOptions(batchSize: 2, retryDelaysSeconds: [10]);
        SynchronizationWakeUp wakeUp = new();
        wakeUp.Notify();
        wakeUp.Notify();
        TestSynchronizationHandler handler = new((_, _) =>
            Task.FromResult(SynchronizationHandlerResult.Completed()));
        await using ServiceProvider provider = CreateWorkerServiceProvider(
            host.ConnectionString,
            timeProvider,
            options,
            wakeUp,
            handler);
        SynchronizationWorker worker = CreateWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            wakeUp,
            timeProvider,
            options);

        int processed =
            await worker.RunWakeUpSignalAsync(TestContext.Current.CancellationToken);

        List<SynchronizationRequest> saved = await ReadAllAsync(host);
        Assert.Equal(3, processed);
        Assert.Equal(3, handler.CallCount);
        Assert.Equal(
            3,
            saved.Count(request => request.Status == SynchronizationStatus.Completed));
        Assert.False(wakeUp.Reader.TryRead(out _));
    }

    [Fact]
    public async Task RunPollingPass_WhenWakeUpSignalIsLost_StillProcessesEligibleWork()
    {
        await using IntegrationSynchronizationSqlTestHost host =
            await IntegrationSynchronizationSqlTestHost.CreateAsync();
        SynchronizationRequest request = CreateRequest();
        await using (IntegrationDbContext dbContext = host.CreateDbContext())
        {
            await SeedAsync(dbContext, request);
        }

        MutableTimeProvider timeProvider = new(NowUtc);
        SynchronizationOptions options =
            CreateOptions(retryDelaysSeconds: [10]);
        SynchronizationWakeUp wakeUp = new();
        TestSynchronizationHandler handler = new((_, _) =>
            Task.FromResult(SynchronizationHandlerResult.Completed()));
        await using ServiceProvider provider = CreateWorkerServiceProvider(
            host.ConnectionString,
            timeProvider,
            options,
            wakeUp,
            handler);
        SynchronizationWorker worker = CreateWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            wakeUp,
            timeProvider,
            options);

        SynchronizationWorker.WorkerPassResult result =
            await worker.RunPollingPassAsync(TestContext.Current.CancellationToken);

        SynchronizationRequest saved = await ReadAsync(host, request.Id);
        Assert.Equal(0, result.RecoveredCount);
        Assert.Equal(1, result.ProcessedCount);
        Assert.Equal(SynchronizationStatus.Completed, saved.Status);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task RunStartupPass_WhenHostShutdownCancelsHandler_PropagatesCancellationAndLeavesRequestProcessing()
    {
        await using IntegrationSynchronizationSqlTestHost host =
            await IntegrationSynchronizationSqlTestHost.CreateAsync();

        SynchronizationRequest request = CreateRequest();

        await using (IntegrationDbContext dbContext = host.CreateDbContext())
        {
            await SeedAsync(dbContext, request);
        }

        TaskCompletionSource handlerStarted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        MutableTimeProvider timeProvider = new(NowUtc);
        SynchronizationOptions options =
            CreateOptions(retryDelaysSeconds: [10]);
        SynchronizationWakeUp wakeUp = new();

        TestSynchronizationHandler handler = new(async (_, cancellationToken) =>
        {
            handlerStarted.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return SynchronizationHandlerResult.Completed();
        });

        await using ServiceProvider provider = CreateWorkerServiceProvider(
            host.ConnectionString,
            timeProvider,
            options,
            wakeUp,
            handler);

        SynchronizationWorker worker = CreateWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            wakeUp,
            timeProvider,
            options);

        using CancellationTokenSource shutdown = new();

        Task<SynchronizationWorker.WorkerPassResult> startupPass =
            worker.RunStartupPassAsync(shutdown.Token);

        await handlerStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        await shutdown.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => startupPass);

        SynchronizationRequest saved = await ReadAsync(host, request.Id);

        Assert.Equal(SynchronizationStatus.Processing, saved.Status);
        Assert.Equal(1, saved.AttemptCount);
        Assert.Equal(NowUtc, saved.ProcessingStartedAtUtc);
        Assert.Null(saved.NextAttemptAtUtc);
        Assert.Null(saved.CompletedAtUtc);
        Assert.Null(saved.LastError);
    }

    private static SynchronizationRequest CreateProcessingRequest(
        int attemptCount,
        DateTimeOffset processingStartedAtUtc)
    {
        SynchronizationRequest request = CreateRequest();
        request.Status = SynchronizationStatus.Processing;
        request.AttemptCount = attemptCount;
        request.ProcessingStartedAtUtc = processingStartedAtUtc;
        return request;
    }
}
