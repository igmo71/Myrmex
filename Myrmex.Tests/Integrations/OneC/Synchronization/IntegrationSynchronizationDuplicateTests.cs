using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Myrmex.Integrations.Persistence;
using Myrmex.Integrations.Persistence.SqlServer;
using Myrmex.Integrations.Synchronization;
using Myrmex.Integrations.Synchronization.Processing;

namespace Myrmex.Tests.Integrations.OneC.Synchronization;

public sealed class IntegrationSynchronizationDuplicateTests
{
    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(4, false)]
    public async Task Store_WhenDuplicate_PreservesLifecycleAndSignalsOnlyPending(
        int statusValue,
        bool expectWakeUp)
    {
        SynchronizationStatus status = (SynchronizationStatus)statusValue;
        await using IntegrationSynchronizationSqlTestHost host =
            await IntegrationSynchronizationSqlTestHost.CreateAsync();
        await using IntegrationDbContext dbContext = host.CreateDbContext();
        SynchronizationRequest existing = CreateRequest(status);
        dbContext.SynchronizationRequests.Add(existing);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        SynchronizationRequest duplicate = CreateRequest();
        SynchronizationWakeUp wakeUp = new();
        SynchronizationRequestStore store = new(
            dbContext,
            wakeUp,
            new SqlServerDuplicateSynchronizationRequestDetector(),
            NullLogger<SynchronizationRequestStore>.Instance);

        SynchronizationRequestIntakeResult result =
            await store.InsertAsync(
                duplicate,
                TestContext.Current.CancellationToken);

        Assert.Equal(SynchronizationRequestIntakeResultKind.Duplicate, result.Kind);
        Assert.Equal(EntityState.Detached, dbContext.Entry(duplicate).State);
        AssertLifecycle(existing, result.Request);
        Assert.Equal(expectWakeUp, wakeUp.Reader.TryRead(out _));

        await using IntegrationDbContext verificationContext = host.CreateDbContext();
        SynchronizationRequest saved = Assert.Single(
            await verificationContext.SynchronizationRequests
                .ToListAsync(TestContext.Current.CancellationToken));
        AssertLifecycle(existing, saved);
    }

    [Fact]
    public async Task Store_WhenInserted_CommitsAndSignals()
    {
        await using IntegrationSynchronizationSqlTestHost host =
            await IntegrationSynchronizationSqlTestHost.CreateAsync();
        await using IntegrationDbContext dbContext = host.CreateDbContext();
        SynchronizationWakeUp wakeUp = new();
        SynchronizationRequestStore store = new(
            dbContext,
            wakeUp,
            new SqlServerDuplicateSynchronizationRequestDetector(),
            NullLogger<SynchronizationRequestStore>.Instance);
        SynchronizationRequest request = CreateRequest();

        SynchronizationRequestIntakeResult result =
            await store.InsertAsync(
                request,
                TestContext.Current.CancellationToken);

        Assert.Equal(SynchronizationRequestIntakeResultKind.Inserted, result.Kind);
        Assert.Same(request, result.Request);
        Assert.True(wakeUp.Reader.TryRead(out _));
        Assert.Equal(
            1,
            await dbContext.SynchronizationRequests.CountAsync(
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Store_WhenPersistenceFailureIsNotIdempotencyDuplicate_Throws()
    {
        await using IntegrationSynchronizationSqlTestHost host =
            await IntegrationSynchronizationSqlTestHost.CreateAsync();
        await using IntegrationDbContext dbContext = host.CreateDbContext();
        SynchronizationWakeUp wakeUp = new();
        SynchronizationRequestStore store = new(
            dbContext,
            wakeUp,
            new SqlServerDuplicateSynchronizationRequestDetector(),
            NullLogger<SynchronizationRequestStore>.Instance);
        SynchronizationRequest request = CreateRequest();
        request.SourceSystem = null!;

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            store.InsertAsync(
                request,
                TestContext.Current.CancellationToken));
        Assert.False(wakeUp.Reader.TryRead(out _));
    }

    private static void AssertLifecycle(
        SynchronizationRequest expected,
        SynchronizationRequest actual)
    {
        Assert.Equal(expected.Status, actual.Status);
        Assert.Equal(expected.AttemptCount, actual.AttemptCount);
        Assert.Equal(expected.ProcessingStartedAtUtc, actual.ProcessingStartedAtUtc);
        Assert.Equal(expected.CompletedAtUtc, actual.CompletedAtUtc);
        Assert.Equal(expected.NextAttemptAtUtc, actual.NextAttemptAtUtc);
        Assert.Equal(expected.LastError, actual.LastError);
    }

    private static SynchronizationRequest CreateRequest(
        SynchronizationStatus status = SynchronizationStatus.Pending) =>
        new()
        {
            SourceSystem = "OneC",
            SourceInstance = "main-infobase",
            EntityType = SynchronizationEntityTypes.ReceivingOrder,
            ExternalId = "80066011-d7c7-11ef-bac8-00155d01d112",
            ExternalDataVersion = [1, 2, 3],
            ExternalDocumentNumber = "UT-00001004",
            ExternalDocumentDate = new DateTime(2025, 1, 21, 10, 15, 36),
            Trigger = SynchronizationTriggers.ChangeNotification,
            Status = status,
            ReceivedAtUtc = DateTimeOffset.Parse("2026-07-14T12:00:00Z"),
            AttemptCount = 2,
            ProcessingStartedAtUtc = status == SynchronizationStatus.Processing
                ? DateTimeOffset.Parse("2026-07-14T12:05:00Z")
                : null,
            CompletedAtUtc = status == SynchronizationStatus.Completed
                ? DateTimeOffset.Parse("2026-07-14T12:10:00Z")
                : null,
            NextAttemptAtUtc = status == SynchronizationStatus.Pending
                ? DateTimeOffset.Parse("2026-07-14T12:15:00Z")
                : null,
            LastError = status is SynchronizationStatus.Failed or
                SynchronizationStatus.Pending
                ? "bounded diagnostic"
                : null
        };
}
