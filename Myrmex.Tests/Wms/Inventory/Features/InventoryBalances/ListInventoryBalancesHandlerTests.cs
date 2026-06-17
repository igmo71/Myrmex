using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;
using Myrmex.Modules.Wms.Inventory.Features.InventoryBalances;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;
using Myrmex.Modules.Wms.Topology.Domain.Zones;
using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Inventory;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Inventory.Features.InventoryBalances;

public sealed class ListInventoryBalancesHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenFiltersMatchNoBalances_ReturnsEmptyList()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        await SeedInventoryBalancesAsync(testDbContext.DbContext);
        ListInventoryBalances.Handler handler = new(testDbContext.DbContext);

        ServiceResult<ListResult<InventoryBalanceDetails>> result = await handler.HandleAsync(
            new ListInventoryBalances.Query
            {
                StockKeepingUnitId = Guid.NewGuid()
            },
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.TotalCount);
        Assert.Empty(result.Value.Items);
    }

    private static async Task<SeededInventoryBalances> SeedInventoryBalancesAsync(WmsDbContext dbContext)
    {
        UnitOfMeasure each = CreateUnitOfMeasure("EA", "Each", "ea");
        UnitOfMeasure caseUnit = CreateUnitOfMeasure("CS", "Case", "cs");
        StockKeepingUnit itemOne = CreateStockKeepingUnit("ITEM-001", "Widget", each.Id);
        StockKeepingUnit itemTwo = CreateStockKeepingUnit("ITEM-002", "Gadget", caseUnit.Id);

        Warehouse warehouseOne = CreateWarehouse("MAIN", "Main Warehouse");
        Warehouse warehouseTwo = CreateWarehouse("SECOND", "Secondary Warehouse");
        Zone warehouseOneZone = CreateZone(warehouseOne.Id, "ZONE-A", "Zone A");
        Zone warehouseTwoZone = CreateZone(warehouseTwo.Id, "ZONE-B", "Zone B");

        StorageLocationType storageLocationType = dbContext.StorageLocationTypes.Single(x => x.Code == "PALLET_RACK");
        StorageLocationStatus storageLocationStatus = dbContext.StorageLocationStatuses.Single(x => x.Code == "AVAILABLE");

        StorageLocation warehouseOnePickLocation = CreateStorageLocation(
            warehouseOne.Id,
            warehouseOneZone.Id,
            storageLocationType.Id,
            storageLocationStatus.Id,
            "A-01-01");
        StorageLocation warehouseOneBulkLocation = CreateStorageLocation(
            warehouseOne.Id,
            warehouseOneZone.Id,
            storageLocationType.Id,
            storageLocationStatus.Id,
            "A-02-01");
        StorageLocation warehouseTwoLocation = CreateStorageLocation(
            warehouseTwo.Id,
            warehouseTwoZone.Id,
            storageLocationType.Id,
            storageLocationStatus.Id,
            "B-01-01");

        InventoryBalance itemOneWarehouseOneBalance = CreateInventoryBalance(
            itemOne.Id,
            warehouseOnePickLocation.Id,
            quantity: 10);
        InventoryBalance itemOneWarehouseTwoBalance = CreateInventoryBalance(
            itemOne.Id,
            warehouseTwoLocation.Id,
            quantity: 0);
        InventoryBalance itemTwoWarehouseOneBalance = CreateInventoryBalance(
            itemTwo.Id,
            warehouseOneBulkLocation.Id,
            quantity: 5);

        dbContext.UnitsOfMeasure.AddRange(each, caseUnit);
        dbContext.StockKeepingUnits.AddRange(itemOne, itemTwo);
        dbContext.Warehouses.AddRange(warehouseOne, warehouseTwo);
        dbContext.Zones.AddRange(warehouseOneZone, warehouseTwoZone);
        dbContext.StorageLocations.AddRange(
            warehouseOnePickLocation,
            warehouseOneBulkLocation,
            warehouseTwoLocation);
        dbContext.InventoryBalances.AddRange(
            itemOneWarehouseOneBalance,
            itemOneWarehouseTwoBalance,
            itemTwoWarehouseOneBalance);

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return new SeededInventoryBalances(
            each,
            caseUnit,
            itemOne,
            itemTwo,
            warehouseOne,
            warehouseTwo,
            warehouseOnePickLocation,
            warehouseOneBulkLocation,
            warehouseTwoLocation,
            itemOneWarehouseOneBalance,
            itemOneWarehouseTwoBalance,
            itemTwoWarehouseOneBalance);
    }

    private static UnitOfMeasure CreateUnitOfMeasure(
        string code,
        string name,
        string symbol)
    {
        var result = UnitOfMeasure.Create(
            code,
            name,
            symbol,
            out UnitOfMeasure? unitOfMeasure);

        Assert.True(result.IsValid);
        Assert.NotNull(unitOfMeasure);
        unitOfMeasure.ClearDomainEvents();

        return unitOfMeasure;
    }

    private static StockKeepingUnit CreateStockKeepingUnit(
        string code,
        string name,
        Guid baseUnitOfMeasureId)
    {
        var result = StockKeepingUnit.Create(
            code,
            name,
            description: null,
            baseUnitOfMeasureId,
            out StockKeepingUnit? stockKeepingUnit);

        Assert.True(result.IsValid);
        Assert.NotNull(stockKeepingUnit);
        stockKeepingUnit.ClearDomainEvents();

        return stockKeepingUnit;
    }

    private static Warehouse CreateWarehouse(
        string code,
        string name)
    {
        var result = Warehouse.Create(
            code,
            name,
            description: null,
            out Warehouse? warehouse);

        Assert.True(result.IsValid);
        Assert.NotNull(warehouse);
        warehouse.ClearDomainEvents();

        return warehouse;
    }

    private static Zone CreateZone(
        Guid warehouseId,
        string code,
        string name)
    {
        var result = Zone.Create(
            warehouseId,
            code,
            name,
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
        string code)
    {
        var result = StorageLocation.Create(
            warehouseId,
            zoneId,
            storageLocationTypeId,
            storageLocationStatusId,
            code,
            name: code,
            description: null,
            isPickable: true,
            out StorageLocation? storageLocation);

        Assert.True(result.IsValid);
        Assert.NotNull(storageLocation);
        storageLocation.ClearDomainEvents();

        return storageLocation;
    }

    private static InventoryBalance CreateInventoryBalance(
        Guid stockKeepingUnitId,
        Guid storageLocationId,
        decimal quantity)
    {
        var result = InventoryBalance.Create(
            stockKeepingUnitId,
            storageLocationId,
            quantity,
            out InventoryBalance? inventoryBalance);

        Assert.True(result.IsValid);
        Assert.NotNull(inventoryBalance);
        inventoryBalance.ClearDomainEvents();

        return inventoryBalance;
    }

    private sealed record SeededInventoryBalances(
        UnitOfMeasure Each,
        UnitOfMeasure CaseUnit,
        StockKeepingUnit ItemOne,
        StockKeepingUnit ItemTwo,
        Warehouse WarehouseOne,
        Warehouse WarehouseTwo,
        StorageLocation WarehouseOnePickLocation,
        StorageLocation WarehouseOneBulkLocation,
        StorageLocation WarehouseTwoLocation,
        InventoryBalance ItemOneWarehouseOneBalance,
        InventoryBalance ItemOneWarehouseTwoBalance,
        InventoryBalance ItemTwoWarehouseOneBalance);
}
