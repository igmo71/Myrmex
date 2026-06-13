using Microsoft.EntityFrameworkCore;
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

public sealed class CreateInventoryBalanceHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenCommandIsInvalid_ReturnsInvalidServiceResult()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();
        CreateInventoryBalance.Handler handler = new(testDbContext.DbContext, domainEventDispatcher);

        CreateInventoryBalance.Command command = new(
            StockKeepingUnitId: Guid.Empty,
            StorageLocationId: Guid.Empty,
            Quantity: -1);

        ServiceResult<InventoryBalanceDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal(ServiceErrorType.Invalid, result.Error.Type);
        Assert.Equal("Validation.Invalid", result.Error.Code);

        Assert.Contains(result.Error.DetailList, error =>
            error.Code == "InventoryBalance.StockKeepingUnitIdRequired" &&
            error.Field == "stockKeepingUnitId");

        Assert.Contains(result.Error.DetailList, error =>
            error.Code == "InventoryBalance.StorageLocationIdRequired" &&
            error.Field == "storageLocationId");

        Assert.Contains(result.Error.DetailList, error =>
            error.Code == "InventoryBalance.QuantityMustBeNonNegative" &&
            error.Field == "quantity");

        Assert.Empty(await testDbContext.DbContext.InventoryBalances.ToListAsync(
            TestContext.Current.CancellationToken));
        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenStockKeepingUnitDoesNotExist_ReturnsNotFound()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();
        StorageLocation storageLocation = await SeedEligibleStorageLocationAsync(testDbContext.DbContext);
        CreateInventoryBalance.Handler handler = new(testDbContext.DbContext, domainEventDispatcher);

        CreateInventoryBalance.Command command = new(
            StockKeepingUnitId: Guid.NewGuid(),
            StorageLocationId: storageLocation.Id,
            Quantity: 10);

        ServiceResult<InventoryBalanceDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal(ServiceErrorType.NotFound, result.Error.Type);
        Assert.Equal("InventoryBalance.StockKeepingUnitNotFound", result.Error.Code);
        Assert.Equal("stockKeepingUnitId", result.Error.Field);
        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenStockKeepingUnitIsInactive_ReturnsInvalid()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();
        SeededReferences references = await SeedValidReferencesAsync(testDbContext.DbContext);
        references.StockKeepingUnit.Deactivate();
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        CreateInventoryBalance.Handler handler = new(testDbContext.DbContext, domainEventDispatcher);

        ServiceResult<InventoryBalanceDetails> result = await handler.HandleAsync(
            CreateCommand(references),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal(ServiceErrorType.Invalid, result.Error.Type);
        Assert.Equal("InventoryBalance.InvalidStockKeepingUnit", result.Error.Code);
        Assert.Equal("stockKeepingUnitId", result.Error.Field);
        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenBaseUnitOfMeasureIsInactive_ReturnsInvalid()
    {
        await using TestWmsDbContext testDbContext =
            await TestWmsDbContext.CreateAsync();

        RecordingDomainEventDispatcher domainEventDispatcher = new();

        SeededReferences references =
            await SeedValidReferencesAsync(testDbContext.DbContext);

        references.BaseUnitOfMeasure.Deactivate();

        await testDbContext.DbContext.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        testDbContext.DbContext.ChangeTracker.Clear();

        CreateInventoryBalance.Handler handler =
            new(testDbContext.DbContext, domainEventDispatcher);

        ServiceResult<InventoryBalanceDetails> result =
            await handler.HandleAsync(
                CreateCommand(references),
                TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal(ServiceErrorType.Invalid, result.Error.Type);
        Assert.Equal(
            "InventoryBalance.InvalidStockKeepingUnit",
            result.Error.Code);
        Assert.Equal("stockKeepingUnitId", result.Error.Field);
        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenStorageLocationDoesNotExist_ReturnsNotFound()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();
        UnitOfMeasure baseUnitOfMeasure = CreateUnitOfMeasure();
        StockKeepingUnit stockKeepingUnit = CreateStockKeepingUnit(baseUnitOfMeasure.Id);
        testDbContext.DbContext.UnitsOfMeasure.Add(baseUnitOfMeasure);
        testDbContext.DbContext.StockKeepingUnits.Add(stockKeepingUnit);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        CreateInventoryBalance.Handler handler = new(testDbContext.DbContext, domainEventDispatcher);

        CreateInventoryBalance.Command command = new(
            StockKeepingUnitId: stockKeepingUnit.Id,
            StorageLocationId: Guid.NewGuid(),
            Quantity: 10);

        ServiceResult<InventoryBalanceDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal(ServiceErrorType.NotFound, result.Error.Type);
        Assert.Equal("InventoryBalance.StorageLocationNotFound", result.Error.Code);
        Assert.Equal("storageLocationId", result.Error.Field);
        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenStorageLocationIsInactive_ReturnsInvalid()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();
        SeededReferences references = await SeedValidReferencesAsync(testDbContext.DbContext);
        references.StorageLocation.Deactivate();
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        CreateInventoryBalance.Handler handler = new(testDbContext.DbContext, domainEventDispatcher);

        ServiceResult<InventoryBalanceDetails> result = await handler.HandleAsync(
            CreateCommand(references),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal(ServiceErrorType.Invalid, result.Error.Type);
        Assert.Equal("InventoryBalance.InvalidStorageLocation", result.Error.Code);
        Assert.Equal("storageLocationId", result.Error.Field);
        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenStorageLocationTypeIsInactive_ReturnsInvalid()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();
        SeededReferences references = await SeedValidReferencesAsync(testDbContext.DbContext);
        references.StorageLocationType.Deactivate();
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        CreateInventoryBalance.Handler handler = new(testDbContext.DbContext, domainEventDispatcher);

        ServiceResult<InventoryBalanceDetails> result = await handler.HandleAsync(
            CreateCommand(references),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal(ServiceErrorType.Invalid, result.Error.Type);
        Assert.Equal("InventoryBalance.InactiveStorageLocationType", result.Error.Code);
        Assert.Equal("storageLocationTypeId", result.Error.Field);
        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenStorageLocationStatusIsInactive_ReturnsInvalid()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();
        SeededReferences references = await SeedValidReferencesAsync(testDbContext.DbContext);
        references.StorageLocationStatus.Deactivate();
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        CreateInventoryBalance.Handler handler = new(testDbContext.DbContext, domainEventDispatcher);

        ServiceResult<InventoryBalanceDetails> result = await handler.HandleAsync(
            CreateCommand(references),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal(ServiceErrorType.Invalid, result.Error.Type);
        Assert.Equal("InventoryBalance.InactiveStorageLocationStatus", result.Error.Code);
        Assert.Equal("storageLocationStatusId", result.Error.Field);
        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenDuplicateStockKeepingUnitStorageLocationExists_ReturnsConflict()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();
        SeededReferences references = await SeedValidReferencesAsync(testDbContext.DbContext);
        CreateInventoryBalance.Handler handler = new(testDbContext.DbContext, domainEventDispatcher);

        ServiceResult<InventoryBalanceDetails> firstResult = await handler.HandleAsync(
            CreateCommand(references),
            TestContext.Current.CancellationToken);

        Assert.True(firstResult.IsSuccess);

        ServiceResult<InventoryBalanceDetails> duplicateResult = await handler.HandleAsync(
            CreateCommand(references, quantity: 20),
            TestContext.Current.CancellationToken);

        Assert.False(duplicateResult.IsSuccess);
        Assert.NotNull(duplicateResult.Error);
        Assert.Equal(ServiceErrorType.Conflict, duplicateResult.Error.Type);
        Assert.Equal("InventoryBalance.DuplicateStockKeepingUnitStorageLocation", duplicateResult.Error.Code);
        Assert.Equal("stockKeepingUnitId", duplicateResult.Error.Field);

        InventoryBalance storedBalance = await testDbContext.DbContext.InventoryBalances.SingleAsync(
            TestContext.Current.CancellationToken);
        Assert.Equal(10, storedBalance.Quantity);
    }

    [Fact]
    public async Task HandleAsync_WhenCommandIsValid_CreatesBalanceAndReturnsDisplayDetails()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();
        SeededReferences references = await SeedValidReferencesAsync(
            testDbContext.DbContext,
            isPickable: false);
        CreateInventoryBalance.Handler handler = new(testDbContext.DbContext, domainEventDispatcher);

        ServiceResult<InventoryBalanceDetails> result = await handler.HandleAsync(
            CreateCommand(references, quantity: 0),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);

        InventoryBalanceDetails details = result.Value;

        Assert.NotEqual(Guid.Empty, details.Id);
        Assert.Equal(references.StockKeepingUnit.Id, details.StockKeepingUnitId);
        Assert.Equal("ITEM-001", details.StockKeepingUnitCode);
        Assert.Equal("Widget", details.StockKeepingUnitName);
        Assert.Equal(references.StorageLocation.Id, details.StorageLocationId);
        Assert.Equal("A-01-01", details.StorageLocationCode);
        Assert.Equal("A-01-01", details.StorageLocationName);
        Assert.Equal(references.Warehouse.Id, details.WarehouseId);
        Assert.Equal("MAIN", details.WarehouseCode);
        Assert.Equal("Main Warehouse", details.WarehouseName);
        Assert.Equal(references.BaseUnitOfMeasure.Id, details.BaseUnitOfMeasureId);
        Assert.Equal("EA", details.BaseUnitOfMeasureCode);
        Assert.Equal("ea", details.BaseUnitOfMeasureSymbol);
        Assert.Equal(0, details.Quantity);
        Assert.Null(details.UpdatedAtUtc);

        InventoryBalance storedBalance = await testDbContext.DbContext.InventoryBalances.SingleAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(details.Id, storedBalance.Id);
        Assert.Equal(references.StockKeepingUnit.Id, storedBalance.StockKeepingUnitId);
        Assert.Equal(references.StorageLocation.Id, storedBalance.StorageLocationId);
        Assert.Equal(0, storedBalance.Quantity);
        Assert.NotEmpty(domainEventDispatcher.DispatchedEvents);
    }

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
