using Microsoft.EntityFrameworkCore;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;
using Myrmex.Modules.Wms.Topology.Domain.Zones;

namespace Myrmex.Tests.Wms.Inventory.Testing;

internal static class InventoryBalanceTestData
{
    internal static async Task<SeededInventoryBalance> SeedInventoryBalanceAsync(
        WmsDbContext dbContext,
        decimal quantity)
    {
        UnitOfMeasure baseUnitOfMeasure = CreateUnitOfMeasure();
        StockKeepingUnit stockKeepingUnit = CreateStockKeepingUnit(baseUnitOfMeasure.Id);
        Warehouse warehouse = CreateWarehouse();
        Zone zone = CreateZone(warehouse.Id);
        StorageLocationType storageLocationType = await dbContext.StorageLocationTypes.SingleAsync(
            x => x.Code == "PALLET_RACK",
            TestContext.Current.CancellationToken);
        StorageLocationStatus storageLocationStatus = await dbContext.StorageLocationStatuses.SingleAsync(
            x => x.Code == "AVAILABLE",
            TestContext.Current.CancellationToken);
        StorageLocation storageLocation = CreateStorageLocation(
            warehouse.Id,
            zone.Id,
            storageLocationType.Id,
            storageLocationStatus.Id);

        var balanceResult = InventoryBalance.Create(
            stockKeepingUnit.Id,
            storageLocation.Id,
            quantity,
            out InventoryBalance? inventoryBalance);

        Assert.True(balanceResult.IsValid);
        Assert.NotNull(inventoryBalance);
        inventoryBalance.ClearDomainEvents();

        dbContext.UnitsOfMeasure.Add(baseUnitOfMeasure);
        dbContext.StockKeepingUnits.Add(stockKeepingUnit);
        dbContext.Warehouses.Add(warehouse);
        dbContext.Zones.Add(zone);
        dbContext.StorageLocations.Add(storageLocation);
        dbContext.InventoryBalances.Add(inventoryBalance);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return new SeededInventoryBalance(
            baseUnitOfMeasure,
            stockKeepingUnit,
            warehouse,
            storageLocation,
            inventoryBalance);
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
        Guid storageLocationStatusId)
    {
        var result = StorageLocation.Create(
            warehouseId,
            zoneId,
            storageLocationTypeId,
            storageLocationStatusId,
            code: "A-01-01",
            name: "A-01-01",
            description: null,
            isPickable: true,
            out StorageLocation? storageLocation);

        Assert.True(result.IsValid);
        Assert.NotNull(storageLocation);
        storageLocation.ClearDomainEvents();

        return storageLocation;
    }
}

internal sealed record SeededInventoryBalance(
    UnitOfMeasure BaseUnitOfMeasure,
    StockKeepingUnit StockKeepingUnit,
    Warehouse Warehouse,
    StorageLocation StorageLocation,
    InventoryBalance InventoryBalance);
