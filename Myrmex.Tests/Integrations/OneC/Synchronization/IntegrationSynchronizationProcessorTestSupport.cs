using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Myrmex.Integrations.Persistence;
using Myrmex.Integrations.Persistence.SqlServer;
using Myrmex.Integrations.Synchronization;
using Myrmex.Integrations.Synchronization.Configuration;
using Myrmex.Integrations.Synchronization.Processing;

namespace Myrmex.Tests.Integrations.OneC.Synchronization;

internal static class IntegrationSynchronizationProcessorTestSupport
{
    public static readonly DateTimeOffset NowUtc =
        DateTimeOffset.Parse("2026-07-14T12:00:00Z");

    public static SynchronizationRequest CreateRequest(
        string entityType = SynchronizationEntityTypes.ReceivingOrder,
        DateTimeOffset? receivedAtUtc = null,
        DateTimeOffset? nextAttemptAtUtc = null) =>
        new()
        {
            SourceSystem = "OneC",
            SourceInstance = "main-infobase",
            EntityType = entityType,
            ExternalId = Guid.NewGuid().ToString("D"),
            ExternalDataVersion = Guid.NewGuid().ToByteArray(),
            Trigger = SynchronizationTriggers.ChangeNotification,
            Status = SynchronizationStatus.Pending,
            ReceivedAtUtc = receivedAtUtc ?? NowUtc,
            NextAttemptAtUtc = nextAttemptAtUtc
        };

    public static async Task SeedAsync(
        IntegrationDbContext dbContext,
        params SynchronizationRequest[] requests)
    {
        dbContext.SynchronizationRequests.AddRange(requests);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    public static async Task<SynchronizationRequest> ReadAsync(
        IntegrationSynchronizationSqlTestHost host,
        Guid requestId)
    {
        await using IntegrationDbContext dbContext = host.CreateDbContext();
        return await dbContext.SynchronizationRequests.SingleAsync(
            request => request.Id == requestId,
            TestContext.Current.CancellationToken);
    }

    public static async Task<List<SynchronizationRequest>> ReadAllAsync(
        IntegrationSynchronizationSqlTestHost host)
    {
        await using IntegrationDbContext dbContext = host.CreateDbContext();
        return await dbContext.SynchronizationRequests
            .OrderBy(request => request.ReceivedAtUtc)
            .ThenBy(request => request.Id)
            .ToListAsync(TestContext.Current.CancellationToken);
    }

    public static SynchronizationProcessor CreateProcessor(
        IntegrationDbContext dbContext,
        MutableTimeProvider timeProvider,
        SynchronizationOptions options,
        params ISynchronizationHandler[] handlers)
    {
        SynchronizationRequestStore store = new(
            dbContext,
            new SynchronizationWakeUp(),
            new SqlServerDuplicateSynchronizationRequestDetector(),
            NullLogger<SynchronizationRequestStore>.Instance);

        return new SynchronizationProcessor(
            store,
            new SynchronizationHandlerResolver(handlers),
            new SynchronizationRetryPolicy(),
            Options.Create(options),
            timeProvider,
            NullLogger<SynchronizationProcessor>.Instance);
    }

    public static SynchronizationRequestStore CreateStore(
        IntegrationDbContext dbContext) =>
        new(
            dbContext,
            new SynchronizationWakeUp(),
            new SqlServerDuplicateSynchronizationRequestDetector(),
            NullLogger<SynchronizationRequestStore>.Instance);

    public static SynchronizationWorker CreateWorker(
        IServiceScopeFactory scopeFactory,
        SynchronizationWakeUp wakeUp,
        MutableTimeProvider timeProvider,
        SynchronizationOptions options) =>
        new(
            scopeFactory,
            wakeUp,
            Options.Create(options),
            timeProvider,
            NullLogger<SynchronizationWorker>.Instance);

    public static ServiceProvider CreateWorkerServiceProvider(
        string connectionString,
        MutableTimeProvider timeProvider,
        SynchronizationOptions options,
        SynchronizationWakeUp wakeUp,
        params ISynchronizationHandler[] handlers)
    {
        ServiceCollection services = new();

        services.AddLogging();
        services.AddDbContext<IntegrationDbContext>(dbOptions =>
            dbOptions.UseSqlServer(connectionString));
        services.AddSingleton<TimeProvider>(timeProvider);
        services.AddSingleton(Options.Create(options));
        services.AddSingleton(wakeUp);
        services.AddSingleton<SynchronizationRetryPolicy>();
        services.AddSingleton<SqlServerDuplicateSynchronizationRequestDetector>();
        services.AddScoped<SynchronizationRequestStore>();
        services.AddScoped<ISynchronizationHandlerResolver, SynchronizationHandlerResolver>();
        services.AddScoped<SynchronizationProcessor>();

        foreach (ISynchronizationHandler handler in handlers)
        {
            services.AddSingleton(handler);
        }

        return services.BuildServiceProvider();
    }

    public static SynchronizationOptions CreateOptions(
        int batchSize = 20,
        int processingAttemptTimeoutSeconds = 30,
        int processingTimeoutSeconds = 300,
        params int[] retryDelaysSeconds) =>
        new()
        {
            PollingIntervalSeconds = 60,
            BatchSize = batchSize,
            ProcessingAttemptTimeoutSeconds = processingAttemptTimeoutSeconds,
            ProcessingTimeoutSeconds = processingTimeoutSeconds,
            RetryDelaysSeconds = [.. retryDelaysSeconds]
        };
}

internal sealed class MutableTimeProvider(DateTimeOffset value) : TimeProvider
{
    private DateTimeOffset _value = value;

    public override DateTimeOffset GetUtcNow() => _value;

    public void SetUtcNow(DateTimeOffset value) => _value = value;
}

internal sealed class TestSynchronizationHandler(
    Func<SynchronizationRequest, CancellationToken, Task<SynchronizationHandlerResult>> handle)
    : ISynchronizationHandler
{
    public string EntityType { get; init; } =
        SynchronizationEntityTypes.ReceivingOrder;

    public int CallCount { get; private set; }

    public async Task<SynchronizationHandlerResult> HandleAsync(
        SynchronizationRequest request,
        CancellationToken cancellationToken)
    {
        CallCount++;
        return await handle(request, cancellationToken);
    }
}
