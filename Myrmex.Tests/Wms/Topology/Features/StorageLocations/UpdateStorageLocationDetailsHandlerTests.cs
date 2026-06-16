using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;
using Myrmex.Modules.Wms.Topology.Domain.Zones;
using Myrmex.Modules.Wms.Topology.Features.StorageLocations;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Topology.Features.StorageLocations;

public sealed class UpdateStorageLocationDetailsHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenCommandIsValid_UpdatesStorageLocationAndReturnsDetails()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        StorageLocation storageLocation = await SeedStorageLocationAsync(testDbContext.DbContext);

        UpdateStorageLocationDetails.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        UpdateStorageLocationDetails.Command command = new(
            StorageLocationId: storageLocation.Id,
            Name: " Updated location ",
            Description: " Updated description ",
            IsPickable: false);

        ServiceResult<StorageLocationDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);

        StorageLocationDetails details = result.Value;

        Assert.Equal(storageLocation.Id, details.Id);
        Assert.Equal(storageLocation.WarehouseId, details.WarehouseId);
        Assert.Equal(storageLocation.ZoneId, details.ZoneId);
        Assert.Equal(storageLocation.StorageLocationTypeId, details.StorageLocationTypeId);
        Assert.Equal(storageLocation.StorageLocationStatusId, details.StorageLocationStatusId);
        Assert.Equal("A-01-01", details.Code);
        Assert.Equal("Updated location", details.Name);
        Assert.Equal("Updated description", details.Description);
        Assert.False(details.IsPickable);
        Assert.True(details.IsActive);

        StorageLocation persistedStorageLocation = await testDbContext.DbContext.StorageLocations.SingleAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal("Updated location", persistedStorageLocation.Name);
        Assert.Equal("Updated description", persistedStorageLocation.Description);
        Assert.False(persistedStorageLocation.IsPickable);

        Assert.NotEmpty(domainEventDispatcher.DispatchedEvents);
    }

    private static async Task<StorageLocation> SeedStorageLocationAsync(
        WmsDbContext dbContext)
    {
        Warehouse warehouse = CreateWarehouse();
        Zone zone = CreateZone(warehouse.Id);

        StorageLocationType type = await GetStorageLocationTypeAsync(dbContext);
        StorageLocationStatus status = await GetStorageLocationStatusAsync(dbContext);

        var createResult = StorageLocation.Create(
            warehouse.Id,
            zone.Id,
            type.Id,
            status.Id,
            code: "A-01-01",
            name: "A-01-01",
            description: null,
            isPickable: true,
            out StorageLocation? storageLocation);

        Assert.True(createResult.IsValid);
        Assert.NotNull(storageLocation);

        dbContext.Warehouses.Add(warehouse);
        dbContext.Zones.Add(zone);
        dbContext.StorageLocations.Add(storageLocation);

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return storageLocation;
    }

    private static Warehouse CreateWarehouse()
    {
        var result = Warehouse.Create(
            code: "MAIN",
            name: "Main Warehouse",
            description: null,
            out Warehouse? warehouse);

        Assert.True(result.IsValid);
        Assert.NotNull(warehouse);

        return warehouse;
    }

    private static Zone CreateZone(Guid warehouseId)
    {
        var result = Zone.Create(
            warehouseId,
            code: "ZONE-A",
            name: "Zone A",
            description: null,
            out Zone? zone);

        Assert.True(result.IsValid);
        Assert.NotNull(zone);

        return zone;
    }

    private static async Task<StorageLocationType> GetStorageLocationTypeAsync(
        WmsDbContext dbContext)
    {
        return await dbContext.StorageLocationTypes.SingleAsync(
            x => x.Code == "PALLET_RACK",
            TestContext.Current.CancellationToken);
    }

    private static async Task<StorageLocationStatus> GetStorageLocationStatusAsync(
        WmsDbContext dbContext)
    {
        return await dbContext.StorageLocationStatuses.SingleAsync(
            x => x.Code == "AVAILABLE",
            TestContext.Current.CancellationToken);
    }
}