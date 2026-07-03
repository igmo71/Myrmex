using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;
using Myrmex.Modules.Wms.Topology.Domain.Zones;
using Myrmex.Modules.Wms.Topology.Features.StorageLocations;
using Myrmex.Shared.Wms.Topology;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Topology.Features.StorageLocations;

public sealed class CreateStorageLocationHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenCommandIsValid_CreatesStorageLocationAndReturnsDetails()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        Warehouse warehouse = CreateWarehouse();
        Zone zone = CreateZone(warehouse.Id);
        StorageLocationType type = await GetStorageLocationTypeAsync(testDbContext.DbContext);
        StorageLocationStatus status = await GetStorageLocationStatusAsync(testDbContext.DbContext);

        testDbContext.DbContext.Warehouses.Add(warehouse);
        testDbContext.DbContext.Zones.Add(zone);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        CreateStorageLocation.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        CreateStorageLocation.Command command = new(
            WarehouseId: warehouse.Id,
            ZoneId: zone.Id,
            StorageLocationTypeId: type.Id,
            StorageLocationStatusId: status.Id,
            Code: " a-01-01 ",
            Name: " A-01-01 ",
            Description: " Pick face ",
            IsPickable: true);

        ServiceResult<StorageLocationDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);

        StorageLocationDetails details = result.Value;

        Assert.NotEqual(Guid.Empty, details.Id);
        Assert.Equal(warehouse.Id, details.WarehouseId);
        Assert.Equal(zone.Id, details.ZoneId);
        Assert.Equal(type.Id, details.StorageLocationTypeId);
        Assert.Equal(status.Id, details.StorageLocationStatusId);
        Assert.Equal("A-01-01", details.Code);
        Assert.Equal("A-01-01", details.Name);
        Assert.Equal("Pick face", details.Description);
        Assert.True(details.IsPickable);
        Assert.True(details.IsActive);

        StorageLocation storageLocation = await testDbContext.DbContext.StorageLocations.SingleAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(details.Id, storageLocation.Id);
        Assert.Equal(warehouse.Id, storageLocation.WarehouseId);
        Assert.Equal(zone.Id, storageLocation.ZoneId);
        Assert.Equal(type.Id, storageLocation.StorageLocationTypeId);
        Assert.Equal(status.Id, storageLocation.StorageLocationStatusId);
        Assert.Equal("A-01-01", storageLocation.Code);
        Assert.Equal("A-01-01", storageLocation.Name);
        Assert.Equal("Pick face", storageLocation.Description);
        Assert.True(storageLocation.IsPickable);

        Assert.NotEmpty(domainEventDispatcher.DispatchedEvents);
    }

    private static Warehouse CreateWarehouse(
        string code = "MAIN",
        string name = "Main Warehouse")
    {
        var result = Warehouse.Create(
            code,
            name,
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
