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
using System.Data.SqlTypes;

namespace Myrmex.Tests.Wms.Inventory.Features.InventoryBalances;

public sealed class ListInventoryBalancesHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenFiltersAreProvided_AppliesSupportedFiltersAndReturnsNestedDetails()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededInventoryBalances seeded = await SeedInventoryBalancesAsync(testDbContext.DbContext);
        ListInventoryBalances.Handler handler = new(testDbContext.DbContext);

        ServiceResult<ListResult<InventoryBalanceDetails>> skuResult = await handler.HandleAsync(
            new ListInventoryBalances.Query
            {
                StockKeepingUnitId = seeded.ItemOne.Id,
                SortBy = InventoryBalanceSortBy.StorageLocationCode
            },
            TestContext.Current.CancellationToken);
        ServiceResult<ListResult<InventoryBalanceDetails>> warehouseResult = await handler.HandleAsync(
            new ListInventoryBalances.Query
            {
                WarehouseId = seeded.WarehouseOne.Id,
                SortBy = InventoryBalanceSortBy.SkuCode
            },
            TestContext.Current.CancellationToken);
        ServiceResult<ListResult<InventoryBalanceDetails>> locationResult = await handler.HandleAsync(
            new ListInventoryBalances.Query
            {
                StorageLocationId = seeded.WarehouseOnePickLocation.Id,
                SortBy = InventoryBalanceSortBy.Quantity
            },
            TestContext.Current.CancellationToken);
        ServiceResult<ListResult<InventoryBalanceDetails>> combinedResult = await handler.HandleAsync(
            new ListInventoryBalances.Query
            {
                StockKeepingUnitId = seeded.ItemOne.Id,
                WarehouseId = seeded.WarehouseOne.Id
            },
            TestContext.Current.CancellationToken);
        ServiceResult<ListResult<InventoryBalanceDetails>> emptyResult = await handler.HandleAsync(
            new ListInventoryBalances.Query
            {
                StockKeepingUnitId = Guid.NewGuid()
            },
            TestContext.Current.CancellationToken);

        Assert.True(skuResult.IsSuccess);
        Assert.Equal(2, skuResult.Value.TotalCount);
        AssertIdsEqual(
            [
                seeded.ItemOneWarehouseOneBalance.Balance.Id,
                seeded.ItemOneWarehouseTwoBalance.Balance.Id
            ],
            skuResult.Value.Items.Select(x => x.Id),
            "SKU filter");

        Assert.True(warehouseResult.IsSuccess);
        Assert.Equal(3, warehouseResult.Value.TotalCount);
        AssertIdsEqual(
            [
                seeded.ItemTwoWarehouseOneBalance.Balance.Id,
                seeded.ItemOneWarehouseOneBalance.Balance.Id,
                seeded.ItemThreeWarehouseOneBalance.Balance.Id
            ],
            warehouseResult.Value.Items.Select(x => x.Id),
            "warehouse filter");

        Assert.True(locationResult.IsSuccess);
        Assert.Equal(2, locationResult.Value.TotalCount);
        AssertIdsEqual(
            [
                seeded.ItemOneWarehouseOneBalance.Balance.Id,
                seeded.ItemThreeWarehouseOneBalance.Balance.Id
            ],
            locationResult.Value.Items.Select(x => x.Id),
            "storage-location filter");

        Assert.True(combinedResult.IsSuccess);
        InventoryBalanceDetails combinedItem = Assert.Single(combinedResult.Value.Items);
        Assert.Equal(seeded.ItemOneWarehouseOneBalance.Balance.Id, combinedItem.Id);
        Assert.Equal(seeded.ItemOne.Id, combinedItem.Sku.Id);
        Assert.Equal(seeded.ItemOne.Code, combinedItem.Sku.Code);
        Assert.Equal(seeded.ItemOne.Name, combinedItem.Sku.Name);
        Assert.Equal(seeded.Each.Id, combinedItem.Sku.BaseUom.Id);
        Assert.Equal(seeded.Each.Code, combinedItem.Sku.BaseUom.Code);
        Assert.Equal(seeded.Each.Symbol, combinedItem.Sku.BaseUom.Symbol);
        Assert.Equal(seeded.WarehouseOnePickLocation.Id, combinedItem.StorageLocation.Id);
        Assert.Equal(seeded.WarehouseOnePickLocation.Code, combinedItem.StorageLocation.Code);
        Assert.Equal(seeded.WarehouseOne.Id, combinedItem.StorageLocation.Warehouse.Id);
        Assert.Equal(seeded.WarehouseOne.Code, combinedItem.StorageLocation.Warehouse.Code);
        Assert.Equal(seeded.WarehouseOne.Name, combinedItem.StorageLocation.Warehouse.Name);

        Assert.True(emptyResult.IsSuccess);
        Assert.Equal(0, emptyResult.Value.TotalCount);
        Assert.Empty(emptyResult.Value.Items);
    }

    [Fact]
    public async Task HandleAsync_WhenPagingIsApplied_ReturnsTotalCountBeforePaging()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededInventoryBalances seeded = await SeedInventoryBalancesAsync(testDbContext.DbContext);
        ListInventoryBalances.Handler handler = new(testDbContext.DbContext);

        ServiceResult<ListResult<InventoryBalanceDetails>> result = await handler.HandleAsync(
            new ListInventoryBalances.Query
            {
                Skip = 1,
                Take = 2,
                SortBy = InventoryBalanceSortBy.SkuCode
            },
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value.TotalCount);
        Assert.Equal(1, result.Value.Skip);
        Assert.Equal(2, result.Value.Take);
        Assert.Equal(2, result.Value.Items.Count);
        AssertIdsEqual(
            ExpectedOrder(seeded, InventoryBalanceSortBy.SkuCode, sortDescending: false)
                .Skip(1)
                .Take(2),
            result.Value.Items.Select(x => x.Id),
            "paged SKU-code sort");
    }

    [Fact]
    public async Task HandleAsync_WhenSortBySupportedKeys_OrdersByRequestedKeyThenId()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededInventoryBalances seeded = await SeedInventoryBalancesAsync(testDbContext.DbContext);
        ListInventoryBalances.Handler handler = new(testDbContext.DbContext);

        (string? SortBy, bool SortDescending)[] sortCases =
        [
            (null, false),
            (null, true),
            (InventoryBalanceSortBy.Quantity, false),
            (InventoryBalanceSortBy.Quantity, true),
            (InventoryBalanceSortBy.SkuCode, false),
            (InventoryBalanceSortBy.SkuCode, true),
            (InventoryBalanceSortBy.SkuName, false),
            (InventoryBalanceSortBy.SkuName, true),
            (InventoryBalanceSortBy.SkuBaseUomSymbol, false),
            (InventoryBalanceSortBy.SkuBaseUomSymbol, true),
            (InventoryBalanceSortBy.StorageLocationCode, false),
            (InventoryBalanceSortBy.StorageLocationCode, true),
            (InventoryBalanceSortBy.WarehouseCode, false),
            (InventoryBalanceSortBy.WarehouseCode, true),
            (InventoryBalanceSortBy.WarehouseName, false),
            (InventoryBalanceSortBy.WarehouseName, true)
        ];

        foreach ((string? sortBy, bool sortDescending) in sortCases)
        {
            ServiceResult<ListResult<InventoryBalanceDetails>> result = await handler.HandleAsync(
                new ListInventoryBalances.Query
                {
                    SortBy = sortBy,
                    SortDescending = sortDescending
                },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess);
            AssertIdsEqual(
                ExpectedOrder(seeded, sortBy, sortDescending),
                result.Value.Items.Select(x => x.Id),
                $"{sortBy ?? "default"} descending={sortDescending}");
        }
    }

    private static IEnumerable<Guid> ExpectedOrder(
    SeededInventoryBalances seeded,
    string? sortBy,
    bool sortDescending)
    {
        IEnumerable<SeededInventoryBalanceItem> ordered = sortBy switch
        {
            InventoryBalanceSortBy.Quantity => sortDescending
                ? seeded.Items
                    .OrderByDescending(x => x.Balance.Quantity)
                    .ThenBy(x => new SqlGuid(x.Balance.Id))
                : seeded.Items
                    .OrderBy(x => x.Balance.Quantity)
                    .ThenBy(x => new SqlGuid(x.Balance.Id)),

            InventoryBalanceSortBy.SkuCode => sortDescending
                ? seeded.Items
                    .OrderByDescending(x => x.StockKeepingUnit.Code)
                    .ThenBy(x => new SqlGuid(x.Balance.Id))
                : seeded.Items
                    .OrderBy(x => x.StockKeepingUnit.Code)
                    .ThenBy(x => new SqlGuid(x.Balance.Id)),

            InventoryBalanceSortBy.SkuName => sortDescending
                ? seeded.Items
                    .OrderByDescending(x => x.StockKeepingUnit.Name)
                    .ThenBy(x => new SqlGuid(x.Balance.Id))
                : seeded.Items
                    .OrderBy(x => x.StockKeepingUnit.Name)
                    .ThenBy(x => new SqlGuid(x.Balance.Id)),

            InventoryBalanceSortBy.SkuBaseUomSymbol => sortDescending
                ? seeded.Items
                    .OrderByDescending(
                        x => x.BaseUnitOfMeasure.Symbol ?? x.BaseUnitOfMeasure.Code)
                    .ThenBy(x => new SqlGuid(x.Balance.Id))
                : seeded.Items
                    .OrderBy(
                        x => x.BaseUnitOfMeasure.Symbol ?? x.BaseUnitOfMeasure.Code)
                    .ThenBy(x => new SqlGuid(x.Balance.Id)),

            InventoryBalanceSortBy.StorageLocationCode => sortDescending
                ? seeded.Items
                    .OrderByDescending(x => x.StorageLocation.Code)
                    .ThenBy(x => new SqlGuid(x.Balance.Id))
                : seeded.Items
                    .OrderBy(x => x.StorageLocation.Code)
                    .ThenBy(x => new SqlGuid(x.Balance.Id)),

            InventoryBalanceSortBy.WarehouseCode => sortDescending
                ? seeded.Items
                    .OrderByDescending(x => x.Warehouse.Code)
                    .ThenBy(x => new SqlGuid(x.Balance.Id))
                : seeded.Items
                    .OrderBy(x => x.Warehouse.Code)
                    .ThenBy(x => new SqlGuid(x.Balance.Id)),

            InventoryBalanceSortBy.WarehouseName => sortDescending
                ? seeded.Items
                    .OrderByDescending(x => x.Warehouse.Name)
                    .ThenBy(x => new SqlGuid(x.Balance.Id))
                : seeded.Items
                    .OrderBy(x => x.Warehouse.Name)
                    .ThenBy(x => new SqlGuid(x.Balance.Id)),

            _ => sortDescending
                ? seeded.Items.OrderByDescending(x => new SqlGuid(x.Balance.Id))
                : seeded.Items.OrderBy(x => new SqlGuid(x.Balance.Id))
        };

        return ordered.Select(x => x.Balance.Id);
    }

    private static void AssertIdsEqual(
        IEnumerable<Guid> expected,
        IEnumerable<Guid> actual,
        string scenario)
    {
        Guid[] expectedIds = expected.ToArray();
        Guid[] actualIds = actual.ToArray();

        Assert.True(
            expectedIds.SequenceEqual(actualIds),
            $"{scenario}: expected {string.Join(", ", expectedIds)}, actual {string.Join(", ", actualIds)}.");
    }

    private static async Task<SeededInventoryBalances> SeedInventoryBalancesAsync(WmsDbContext dbContext)
    {
        UnitOfMeasure each = CreateUnitOfMeasure("EA", "Each", "ea");
        UnitOfMeasure caseUnit = CreateUnitOfMeasure("CS", "Case", "cs");
        UnitOfMeasure pack = CreateUnitOfMeasure("PK", "Pack", null);
        StockKeepingUnit itemOne = CreateStockKeepingUnit("SKU-B", "Beta Widget", each.Id);
        StockKeepingUnit itemTwo = CreateStockKeepingUnit("SKU-A", "Alpha Widget", caseUnit.Id);
        StockKeepingUnit itemThree = CreateStockKeepingUnit("SKU-C", "Gamma Widget", pack.Id);

        Warehouse warehouseOne = CreateWarehouse("WH-B", "Beta Warehouse");
        Warehouse warehouseTwo = CreateWarehouse("WH-A", "Alpha Warehouse");
        Zone warehouseOneZone = CreateZone(warehouseOne.Id, "ZONE-B", "Zone B");
        Zone warehouseTwoZone = CreateZone(warehouseTwo.Id, "ZONE-A", "Zone A");

        StorageLocationType storageLocationType = dbContext.StorageLocationTypes.Single(x => x.Code == "PALLET_RACK");
        StorageLocationStatus storageLocationStatus = dbContext.StorageLocationStatuses.Single(x => x.Code == "AVAILABLE");

        StorageLocation warehouseOnePickLocation = CreateStorageLocation(
            warehouseOne.Id,
            warehouseOneZone.Id,
            storageLocationType.Id,
            storageLocationStatus.Id,
            "LOC-B");
        StorageLocation warehouseOneBulkLocation = CreateStorageLocation(
            warehouseOne.Id,
            warehouseOneZone.Id,
            storageLocationType.Id,
            storageLocationStatus.Id,
            "LOC-A");
        StorageLocation warehouseTwoLocation = CreateStorageLocation(
            warehouseTwo.Id,
            warehouseTwoZone.Id,
            storageLocationType.Id,
            storageLocationStatus.Id,
            "LOC-C");

        InventoryBalance itemOneWarehouseOneBalance = CreateInventoryBalance(
            itemOne.Id,
            warehouseOnePickLocation.Id,
            quantity: 5);
        InventoryBalance itemOneWarehouseTwoBalance = CreateInventoryBalance(
            itemOne.Id,
            warehouseTwoLocation.Id,
            quantity: 0);
        InventoryBalance itemTwoWarehouseOneBalance = CreateInventoryBalance(
            itemTwo.Id,
            warehouseOneBulkLocation.Id,
            quantity: 5);
        InventoryBalance itemThreeWarehouseOneBalance = CreateInventoryBalance(
            itemThree.Id,
            warehouseOnePickLocation.Id,
            quantity: 12);

        dbContext.UnitsOfMeasure.AddRange(each, caseUnit, pack);
        dbContext.StockKeepingUnits.AddRange(itemOne, itemTwo, itemThree);
        dbContext.Warehouses.AddRange(warehouseOne, warehouseTwo);
        dbContext.Zones.AddRange(warehouseOneZone, warehouseTwoZone);
        dbContext.StorageLocations.AddRange(
            warehouseOnePickLocation,
            warehouseOneBulkLocation,
            warehouseTwoLocation);
        dbContext.InventoryBalances.AddRange(
            itemOneWarehouseOneBalance,
            itemOneWarehouseTwoBalance,
            itemTwoWarehouseOneBalance,
            itemThreeWarehouseOneBalance);

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        SeededInventoryBalanceItem itemOneWarehouseOne = new(
            each,
            itemOne,
            warehouseOne,
            warehouseOnePickLocation,
            itemOneWarehouseOneBalance);
        SeededInventoryBalanceItem itemOneWarehouseTwo = new(
            each,
            itemOne,
            warehouseTwo,
            warehouseTwoLocation,
            itemOneWarehouseTwoBalance);
        SeededInventoryBalanceItem itemTwoWarehouseOne = new(
            caseUnit,
            itemTwo,
            warehouseOne,
            warehouseOneBulkLocation,
            itemTwoWarehouseOneBalance);
        SeededInventoryBalanceItem itemThreeWarehouseOne = new(
            pack,
            itemThree,
            warehouseOne,
            warehouseOnePickLocation,
            itemThreeWarehouseOneBalance);

        return new SeededInventoryBalances(
            each,
            caseUnit,
            pack,
            itemOne,
            itemTwo,
            itemThree,
            warehouseOne,
            warehouseTwo,
            warehouseOnePickLocation,
            warehouseOneBulkLocation,
            warehouseTwoLocation,
            itemOneWarehouseOne,
            itemOneWarehouseTwo,
            itemTwoWarehouseOne,
            itemThreeWarehouseOne);
    }

    private static UnitOfMeasure CreateUnitOfMeasure(
        string code,
        string name,
        string? symbol)
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

    private sealed record SeededInventoryBalanceItem(
        UnitOfMeasure BaseUnitOfMeasure,
        StockKeepingUnit StockKeepingUnit,
        Warehouse Warehouse,
        StorageLocation StorageLocation,
        InventoryBalance Balance);

    private sealed record SeededInventoryBalances(
        UnitOfMeasure Each,
        UnitOfMeasure CaseUnit,
        UnitOfMeasure Pack,
        StockKeepingUnit ItemOne,
        StockKeepingUnit ItemTwo,
        StockKeepingUnit ItemThree,
        Warehouse WarehouseOne,
        Warehouse WarehouseTwo,
        StorageLocation WarehouseOnePickLocation,
        StorageLocation WarehouseOneBulkLocation,
        StorageLocation WarehouseTwoLocation,
        SeededInventoryBalanceItem ItemOneWarehouseOneBalance,
        SeededInventoryBalanceItem ItemOneWarehouseTwoBalance,
        SeededInventoryBalanceItem ItemTwoWarehouseOneBalance,
        SeededInventoryBalanceItem ItemThreeWarehouseOneBalance)
    {
        public IReadOnlyList<SeededInventoryBalanceItem> Items =>
        [
            ItemOneWarehouseOneBalance,
            ItemOneWarehouseTwoBalance,
            ItemTwoWarehouseOneBalance,
            ItemThreeWarehouseOneBalance
        ];
    }
}
