using Myrmex.Core.Application.Queries;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;
using Myrmex.Modules.Wms.Inventory.Features.InventoryBalances;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;
using Myrmex.Modules.Wms.Topology.Domain.Zones;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Inventory.Features.InventoryBalances;

public sealed class ListInventoryBalancesHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenNoFilters_ReturnsBalancesIncludingZeroQuantityWithDisplayContext()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededInventoryBalances seeded = await SeedInventoryBalancesAsync(testDbContext.DbContext);
        ListInventoryBalances.Handler handler = new(testDbContext.DbContext);

        ServiceResult<ListResult<InventoryBalanceDetails>> result = await handler.HandleAsync(
            new ListInventoryBalances.Query(),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.TotalCount);
        Assert.Equal([0m, 5m, 10m], result.Value.Items.Select(x => x.Quantity).Order().ToArray());

        InventoryBalanceDetails zeroQuantityDetails = Assert.Single(
            result.Value.Items,
            x => x.Id == seeded.ItemOneWarehouseTwoBalance.Id);

        Assert.Equal(0, zeroQuantityDetails.Quantity);
        Assert.Equal(seeded.ItemOne.Id, zeroQuantityDetails.StockKeepingUnitId);
        Assert.Equal("ITEM-001", zeroQuantityDetails.StockKeepingUnitCode);
        Assert.Equal("Widget", zeroQuantityDetails.StockKeepingUnitName);
        Assert.Equal(seeded.WarehouseTwoLocation.Id, zeroQuantityDetails.StorageLocationId);
        Assert.Equal("B-01-01", zeroQuantityDetails.StorageLocationCode);
        Assert.Equal(seeded.WarehouseTwo.Id, zeroQuantityDetails.WarehouseId);
        Assert.Equal("SECOND", zeroQuantityDetails.WarehouseCode);
        Assert.Equal(seeded.Each.Id, zeroQuantityDetails.BaseUnitOfMeasureId);
        Assert.Equal("EA", zeroQuantityDetails.BaseUnitOfMeasureCode);
    }

    [Fact]
    public async Task HandleAsync_WhenPagingIsProvided_ReturnsBoundedNoFilterResults()
    {
        await using TestWmsDbContext testDbContext =
            await TestWmsDbContext.CreateAsync();

        await SeedInventoryBalancesAsync(testDbContext.DbContext);

        ListInventoryBalances.Handler handler =
            new(testDbContext.DbContext);

        ServiceResult<ListResult<InventoryBalanceDetails>> result =
            await handler.HandleAsync(
                new ListInventoryBalances.Query
                {
                    Skip = 1,
                    Take = 1,
                    SortBy = "quantity"
                },
                TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.TotalCount);
        Assert.Equal(1, result.Value.Skip);
        Assert.Equal(1, result.Value.Take);

        InventoryBalanceDetails details =
            Assert.Single(result.Value.Items);

        Assert.Equal(5m, details.Quantity);
    }

    [Fact]
    public async Task HandleAsync_WhenStockKeepingUnitFilterIsProvided_ReturnsOnlyThatSkuBalances()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededInventoryBalances seeded = await SeedInventoryBalancesAsync(testDbContext.DbContext);
        ListInventoryBalances.Handler handler = new(testDbContext.DbContext);

        ServiceResult<ListResult<InventoryBalanceDetails>> result = await handler.HandleAsync(
            new ListInventoryBalances.Query
            {
                StockKeepingUnitId = seeded.ItemOne.Id
            },
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.All(result.Value.Items, x => Assert.Equal(seeded.ItemOne.Id, x.StockKeepingUnitId));
        Assert.Equal([0, 10], result.Value.Items.Select(x => x.Quantity).Order().ToArray());
    }

    [Fact]
    public async Task HandleAsync_WhenStorageLocationFilterIsProvided_ReturnsOnlyThatLocationBalance()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededInventoryBalances seeded = await SeedInventoryBalancesAsync(testDbContext.DbContext);
        ListInventoryBalances.Handler handler = new(testDbContext.DbContext);

        ServiceResult<ListResult<InventoryBalanceDetails>> result = await handler.HandleAsync(
            new ListInventoryBalances.Query
            {
                StorageLocationId = seeded.WarehouseOneBulkLocation.Id
            },
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);

        InventoryBalanceDetails details = Assert.Single(result.Value.Items);
        Assert.Equal(seeded.ItemTwo.Id, details.StockKeepingUnitId);
        Assert.Equal(seeded.WarehouseOneBulkLocation.Id, details.StorageLocationId);
        Assert.Equal(seeded.ItemTwoWarehouseOneBalance.Id, details.Id);
    }

    [Fact]
    public async Task HandleAsync_WhenWarehouseFilterIsProvided_ReturnsOnlyBalancesInThatWarehouse()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededInventoryBalances seeded = await SeedInventoryBalancesAsync(testDbContext.DbContext);
        ListInventoryBalances.Handler handler = new(testDbContext.DbContext);

        ServiceResult<ListResult<InventoryBalanceDetails>> result = await handler.HandleAsync(
            new ListInventoryBalances.Query
            {
                WarehouseId = seeded.WarehouseOne.Id
            },
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.All(result.Value.Items, x => Assert.Equal(seeded.WarehouseOne.Id, x.WarehouseId));
        Assert.Equal(new HashSet<Guid>
        {
            seeded.ItemOneWarehouseOneBalance.Id,
            seeded.ItemTwoWarehouseOneBalance.Id
        },
        result.Value.Items.Select(x => x.Id).ToHashSet());
    }

    [Fact]
    public async Task HandleAsync_WhenStockKeepingUnitAndWarehouseFiltersAreProvided_ReturnsSkuBalanceInWarehouse()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededInventoryBalances seeded = await SeedInventoryBalancesAsync(testDbContext.DbContext);
        ListInventoryBalances.Handler handler = new(testDbContext.DbContext);

        ServiceResult<ListResult<InventoryBalanceDetails>> result = await handler.HandleAsync(
            new ListInventoryBalances.Query
            {
                StockKeepingUnitId = seeded.ItemOne.Id,
                WarehouseId = seeded.WarehouseOne.Id
            },
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);

        InventoryBalanceDetails details = Assert.Single(result.Value.Items);
        Assert.Equal(seeded.ItemOneWarehouseOneBalance.Id, details.Id);
        Assert.Equal(seeded.ItemOne.Id, details.StockKeepingUnitId);
        Assert.Equal(seeded.WarehouseOne.Id, details.WarehouseId);
        Assert.Equal(seeded.WarehouseOnePickLocation.Id, details.StorageLocationId);
    }

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
