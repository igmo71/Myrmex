using Microsoft.EntityFrameworkCore;
using Myrmex.Integrations.Persistence;
using Myrmex.Integrations.Synchronization;

namespace Myrmex.Tests.Integrations.OneC.Synchronization;

public sealed class IntegrationSynchronizationIdempotencyTests
{
    [Fact]
    public async Task UniqueIndex_RejectsSameSourceEntityAndVersion()
    {
        await using IntegrationSynchronizationSqlTestHost host =
            await IntegrationSynchronizationSqlTestHost.CreateAsync();
        await using IntegrationDbContext dbContext = host.CreateDbContext();

        dbContext.SynchronizationRequests.Add(CreateRequest());
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        dbContext.SynchronizationRequests.Add(CreateRequest());

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            dbContext.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UniqueIndex_AllowsDifferentDataVersionAndSourceInstance()
    {
        await using IntegrationSynchronizationSqlTestHost host =
            await IntegrationSynchronizationSqlTestHost.CreateAsync();
        await using IntegrationDbContext dbContext = host.CreateDbContext();

        dbContext.SynchronizationRequests.AddRange(
            CreateRequest(),
            CreateRequest(dataVersion: [9, 8, 7]),
            CreateRequest(sourceInstance: "secondary-infobase"));

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(
            3,
            await dbContext.SynchronizationRequests.CountAsync(
                TestContext.Current.CancellationToken));
    }

    private static SynchronizationRequest CreateRequest(
        string sourceInstance = "main-infobase",
        byte[]? dataVersion = null) =>
        new()
        {
            SourceSystem = "OneC",
            SourceInstance = sourceInstance,
            EntityType = SynchronizationEntityTypes.ReceivingOrder,
            ExternalId = "80066011-d7c7-11ef-bac8-00155d01d112",
            ExternalDataVersion = dataVersion ?? [1, 2, 3],
            Trigger = SynchronizationTriggers.ChangeNotification,
            Status = SynchronizationStatus.Pending,
            ReceivedAtUtc = DateTimeOffset.Parse("2026-07-14T12:00:00Z")
        };
}
