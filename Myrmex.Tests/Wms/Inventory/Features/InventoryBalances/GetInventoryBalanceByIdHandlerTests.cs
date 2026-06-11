using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;
using Myrmex.Modules.Wms.Inventory.Features.InventoryBalances;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;
using Myrmex.Modules.Wms.Topology.Domain.Zones;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Inventory.Features.InventoryBalances;

public sealed class GetInventoryBalanceByIdHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenInventoryBalanceExists_ReturnsDisplayContext()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededInventoryBalance seeded = await SeedInventoryBalanceAsync(testDbContext.DbContext, quantity: 10);

        GetInventoryBalanceById.Handler handler = new(testDbContext.DbContext);

        ServiceResult<InventoryBalanceDetails> result = await handler.HandleAsync(
            new GetInventoryBalanceById.Query(seeded.InventoryBalance.Id),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);

        InventoryBalanceDetails details = result.Value;

        Assert.Equal(seeded.InventoryBalance.Id, details.Id);
        Assert.Equal(seeded.StockKeepingUnit.Id, details.StockKeepingUnitId);
        Assert.Equal("ITEM-001", details.StockKeepingUnitCode);
        Assert.Equal("Widget", details.StockKeepingUnitName);
        Assert.Equal(seeded.StorageLocation.Id, details.StorageLocationId);
        Assert.Equal("A-01-01", details.StorageLocationCode);
        Assert.Equal("A-01-01", details.StorageLocationName);
        Assert.Equal(seeded.Warehouse.Id, details.WarehouseId);
        Assert.Equal("MAIN", details.WarehouseCode);
        Assert.Equal("Main Warehouse", details.WarehouseName);
        Assert.Equal(seeded.BaseUnitOfMeasure.Id, details.BaseUnitOfMeasureId);
        Assert.Equal("EA", details.BaseUnitOfMeasureCode);
        Assert.Equal("ea", details.BaseUnitOfMeasureSymbol);
        Assert.Equal(10, details.Quantity);
        Assert.Equal(seeded.InventoryBalance.CreatedAtUtc, details.CreatedAtUtc);
        Assert.Null(details.UpdatedAtUtc);
    }

    [Fact]
    public async Task HandleAsync_WhenInventoryBalanceHasZeroQuantity_ReturnsZeroQuantity()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededInventoryBalance seeded = await SeedInventoryBalanceAsync(testDbContext.DbContext, quantity: 0);

        GetInventoryBalanceById.Handler handler = new(testDbContext.DbContext);

        ServiceResult<InventoryBalanceDetails> result = await handler.HandleAsync(
            new GetInventoryBalanceById.Query(seeded.InventoryBalance.Id),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.Quantity);
    }

    [Fact]
    public async Task HandleAsync_WhenInventoryBalanceWasUpdated_ReturnsUpdatedTimestamp()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededInventoryBalance seeded = await SeedInventoryBalanceAsync(testDbContext.DbContext, quantity: 10);

        var updateResult = seeded.InventoryBalance.UpdateQuantity(5);
        Assert.True(updateResult.IsValid);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        GetInventoryBalanceById.Handler handler = new(testDbContext.DbContext);

        ServiceResult<InventoryBalanceDetails> result = await handler.HandleAsync(
            new GetInventoryBalanceById.Query(seeded.InventoryBalance.Id),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.Quantity);
        Assert.NotNull(result.Value.UpdatedAtUtc);
        Assert.Equal(seeded.InventoryBalance.UpdatedAtUtc, result.Value.UpdatedAtUtc);
    }

    [Fact]
    public async Task HandleAsync_WhenReferencedRecordsAreInactive_ReturnsDisplayContext()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededInventoryBalance seeded = await SeedInventoryBalanceAsync(testDbContext.DbContext, quantity: 10);

        seeded.StockKeepingUnit.Deactivate();
        seeded.StorageLocation.Deactivate();
        seeded.BaseUnitOfMeasure.Deactivate();
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        GetInventoryBalanceById.Handler handler = new(testDbContext.DbContext);

        ServiceResult<InventoryBalanceDetails> result = await handler.HandleAsync(
            new GetInventoryBalanceById.Query(seeded.InventoryBalance.Id),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("ITEM-001", result.Value.StockKeepingUnitCode);
        Assert.Equal("A-01-01", result.Value.StorageLocationCode);
        Assert.Equal("EA", result.Value.BaseUnitOfMeasureCode);
    }

    [Fact]
    public async Task HandleAsync_WhenInventoryBalanceDoesNotExist_ReturnsNotFound()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        GetInventoryBalanceById.Handler handler = new(testDbContext.DbContext);

        ServiceResult<InventoryBalanceDetails> result = await handler.HandleAsync(
            new GetInventoryBalanceById.Query(Guid.NewGuid()),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal(ServiceErrorType.NotFound, result.Error.Type);
        Assert.Equal("InventoryBalance.NotFound", result.Error.Code);
        Assert.Equal("Inventory balance was not found.", result.Error.Message);
    }

    private static async Task<SeededInventoryBalance> SeedInventoryBalanceAsync(
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

    private sealed record SeededInventoryBalance(
        UnitOfMeasure BaseUnitOfMeasure,
        StockKeepingUnit StockKeepingUnit,
        Warehouse Warehouse,
        StorageLocation StorageLocation,
        InventoryBalance InventoryBalance);
}
