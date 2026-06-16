using Microsoft.EntityFrameworkCore;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Inventory.Features.InventoryBalances;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;
using Myrmex.Modules.Wms.Topology.Domain.Zones;

namespace Myrmex.Tests.Wms.Inventory.Features.InventoryBalances;

public sealed class CreateInventoryBalanceHandlerTests
{
    private static CreateInventoryBalance.Command CreateCommand(
        SeededReferences references,
        decimal quantity = 10)
    {
        return new CreateInventoryBalance.Command(
            references.StockKeepingUnit.Id,
            references.StorageLocation.Id,
            quantity);
    }
    private static async Task<SeededReferences> SeedValidReferencesAsync(
        WmsDbContext dbContext,
        bool isPickable = true)
    {
        UnitOfMeasure baseUnitOfMeasure = CreateUnitOfMeasure();
        StockKeepingUnit stockKeepingUnit = CreateStockKeepingUnit(baseUnitOfMeasure.Id);
        Warehouse warehouse = CreateWarehouse();
        Zone zone = CreateZone(warehouse.Id);
        StorageLocationType storageLocationType = await GetStorageLocationTypeAsync(dbContext);
        StorageLocationStatus storageLocationStatus = await GetStorageLocationStatusAsync(dbContext);
        StorageLocation storageLocation = CreateStorageLocation(
            warehouse.Id,
            zone.Id,
            storageLocationType.Id,
            storageLocationStatus.Id,
            isPickable);

        dbContext.UnitsOfMeasure.Add(baseUnitOfMeasure);
        dbContext.StockKeepingUnits.Add(stockKeepingUnit);
        dbContext.Warehouses.Add(warehouse);
        dbContext.Zones.Add(zone);
        dbContext.StorageLocations.Add(storageLocation);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return new SeededReferences(
            baseUnitOfMeasure,
            stockKeepingUnit,
            warehouse,
            zone,
            storageLocationType,
            storageLocationStatus,
            storageLocation);
    }

    private static async Task<StorageLocation> SeedEligibleStorageLocationAsync(WmsDbContext dbContext)
    {
        Warehouse warehouse = CreateWarehouse();
        Zone zone = CreateZone(warehouse.Id);
        StorageLocationType storageLocationType = await GetStorageLocationTypeAsync(dbContext);
        StorageLocationStatus storageLocationStatus = await GetStorageLocationStatusAsync(dbContext);
        StorageLocation storageLocation = CreateStorageLocation(
            warehouse.Id,
            zone.Id,
            storageLocationType.Id,
            storageLocationStatus.Id,
            isPickable: true);

        dbContext.Warehouses.Add(warehouse);
        dbContext.Zones.Add(zone);
        dbContext.StorageLocations.Add(storageLocation);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return storageLocation;
    }

    private static UnitOfMeasure CreateUnitOfMeasure()
    {
        var result = UnitOfMeasure.Create(
            code: "EA",
            name: "Each",
            symbol: "ea",
            out UnitOfMeasure? unitOfMeasure);

        Assert.True(result.IsValid);
        Assert.NotNull(unitOfMeasure);
        unitOfMeasure.ClearDomainEvents();

        return unitOfMeasure;
    }

    private static StockKeepingUnit CreateStockKeepingUnit(Guid baseUnitOfMeasureId)
    {
        var result = StockKeepingUnit.Create(
            code: "ITEM-001",
            name: "Widget",
            description: null,
            baseUnitOfMeasureId,
            out StockKeepingUnit? stockKeepingUnit);

        Assert.True(result.IsValid);
        Assert.NotNull(stockKeepingUnit);
        stockKeepingUnit.ClearDomainEvents();

        return stockKeepingUnit;
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
        warehouse.ClearDomainEvents();

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
        zone.ClearDomainEvents();

        return zone;
    }

    private static StorageLocation CreateStorageLocation(
        Guid warehouseId,
        Guid zoneId,
        Guid storageLocationTypeId,
        Guid storageLocationStatusId,
        bool isPickable)
    {
        var result = StorageLocation.Create(
            warehouseId,
            zoneId,
            storageLocationTypeId,
            storageLocationStatusId,
            code: "A-01-01",
            name: "A-01-01",
            description: null,
            isPickable,
            out StorageLocation? storageLocation);

        Assert.True(result.IsValid);
        Assert.NotNull(storageLocation);
        storageLocation.ClearDomainEvents();

        return storageLocation;
    }

    private static async Task<StorageLocationType> GetStorageLocationTypeAsync(WmsDbContext dbContext)
    {
        return await dbContext.StorageLocationTypes.SingleAsync(
            x => x.Code == "PALLET_RACK",
            TestContext.Current.CancellationToken);
    }

    private static async Task<StorageLocationStatus> GetStorageLocationStatusAsync(WmsDbContext dbContext)
    {
        return await dbContext.StorageLocationStatuses.SingleAsync(
            x => x.Code == "AVAILABLE",
            TestContext.Current.CancellationToken);
    }

    private sealed record SeededReferences(
        UnitOfMeasure BaseUnitOfMeasure,
        StockKeepingUnit StockKeepingUnit,
        Warehouse Warehouse,
        Zone Zone,
        StorageLocationType StorageLocationType,
        StorageLocationStatus StorageLocationStatus,
        StorageLocation StorageLocation);
}
