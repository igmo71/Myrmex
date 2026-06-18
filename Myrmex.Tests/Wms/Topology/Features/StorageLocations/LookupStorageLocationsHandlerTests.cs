using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;
using Myrmex.Modules.Wms.Topology.Domain.Zones;
using Myrmex.Modules.Wms.Topology.Features.StorageLocations;
using Myrmex.Shared.Wms.Topology;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Topology.Features.StorageLocations;

public sealed class LookupStorageLocationsHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenSearchTextMatchesCodeOrName_ReturnsWarehouseScopedMatchesOrderedByCode()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededTopology seeded = await SeedTopologyAsync(testDbContext.DbContext);

        await AddStorageLocationAsync(testDbContext, seeded.WarehouseOne, seeded.ZoneOne, "SKU-WIDGET", "Other");
        await AddStorageLocationAsync(testDbContext, seeded.WarehouseOne, seeded.ZoneOne, "BBB-001", "Widget");
        await AddStorageLocationAsync(testDbContext, seeded.WarehouseOne, seeded.ZoneOne, "CCC-001", "Other");
        await AddStorageLocationAsync(testDbContext, seeded.WarehouseTwo, seeded.ZoneTwo, "AAA-OTHER", "Widget");

        LookupStorageLocations.Handler handler = new(testDbContext.DbContext);

        ServiceResult<IReadOnlyList<StorageLocationLookupItem>> result = await handler.HandleAsync(
            new LookupStorageLocations.Query
            {
                WarehouseId = seeded.WarehouseOne.Id,
                SearchText = "Widget",
                SelectableOnly = true
            },
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(["BBB-001", "SKU-WIDGET"], result.Value.Select(x => x.Code).ToArray());
        Assert.All(result.Value, item => Assert.Equal(seeded.WarehouseOne.Id, item.WarehouseId));
    }

    [Fact]
    public async Task HandleAsync_WhenTakeExceedsMaximum_ReturnsAtMostTwentyItems()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededTopology seeded = await SeedTopologyAsync(testDbContext.DbContext);

        for (int index = 1; index <= 25; index++)
        {
            await AddStorageLocationAsync(
                testDbContext,
                seeded.WarehouseOne,
                seeded.ZoneOne,
                $"LOC-{index:000}",
                $"Location {index:000}");
        }

        LookupStorageLocations.Handler handler = new(testDbContext.DbContext);

        ServiceResult<IReadOnlyList<StorageLocationLookupItem>> result = await handler.HandleAsync(
            new LookupStorageLocations.Query
            {
                WarehouseId = seeded.WarehouseOne.Id,
                Take = 1_000,
                SelectableOnly = true
            },
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(20, result.Value.Count);
        Assert.Equal("LOC-001", result.Value[0].Code);
        Assert.Equal("LOC-020", result.Value[^1].Code);
    }

    [Fact]
    public async Task HandleAsync_WhenSelectableOnlyIsTrue_ReturnsOnlyLocationsThatMatchCreateEligibility()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededTopology seeded = await SeedTopologyAsync(testDbContext.DbContext);

        await AddStorageLocationAsync(testDbContext, seeded.WarehouseOne, seeded.ZoneOne, "ACTIVE", "Active");
        await AddStorageLocationAsync(testDbContext, seeded.WarehouseOne, seeded.ZoneOne, "INACTIVE-LOC", "Inactive location", isActive: false);
        await AddStorageLocationAsync(testDbContext, seeded.WarehouseOne, seeded.ZoneOne, "INACTIVE-TYPE", "Inactive type", isTypeActive: false);
        await AddStorageLocationAsync(testDbContext, seeded.WarehouseOne, seeded.ZoneOne, "INACTIVE-STATUS", "Inactive status", isStatusActive: false);

        LookupStorageLocations.Handler handler = new(testDbContext.DbContext);

        ServiceResult<IReadOnlyList<StorageLocationLookupItem>> selectableResult = await handler.HandleAsync(
            new LookupStorageLocations.Query
            {
                WarehouseId = seeded.WarehouseOne.Id,
                SelectableOnly = true
            },
            TestContext.Current.CancellationToken);

        ServiceResult<IReadOnlyList<StorageLocationLookupItem>> filterResult = await handler.HandleAsync(
            new LookupStorageLocations.Query
            {
                WarehouseId = seeded.WarehouseOne.Id,
                SelectableOnly = false
            },
            TestContext.Current.CancellationToken);

        Assert.True(selectableResult.IsSuccess);
        Assert.Equal(["ACTIVE"], selectableResult.Value.Select(x => x.Code).ToArray());

        Assert.True(filterResult.IsSuccess);
        Assert.Equal(
            ["ACTIVE", "INACTIVE-LOC", "INACTIVE-STATUS", "INACTIVE-TYPE"],
            filterResult.Value.Select(x => x.Code).ToArray());
        Assert.Contains(filterResult.Value, x => x.Code == "INACTIVE-LOC" && !x.IsActive);
    }

    [Fact]
    public async Task HandleAsync_WhenWarehouseDoesNotExist_ReturnsFailure()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        LookupStorageLocations.Handler handler = new(testDbContext.DbContext);

        ServiceResult<IReadOnlyList<StorageLocationLookupItem>> result = await handler.HandleAsync(
            new LookupStorageLocations.Query
            {
                WarehouseId = Guid.NewGuid()
            },
            TestContext.Current.CancellationToken);

        Assert.True(!result.IsSuccess);
    }

    private static async Task<SeededTopology> SeedTopologyAsync(WmsDbContext dbContext)
    {
        Warehouse warehouseOne = CreateWarehouse("WH-ONE", "Warehouse One");
        Warehouse warehouseTwo = CreateWarehouse("WH-TWO", "Warehouse Two");
        Zone zoneOne = CreateZone(warehouseOne.Id, "ZONE-ONE", "Zone One");
        Zone zoneTwo = CreateZone(warehouseTwo.Id, "ZONE-TWO", "Zone Two");

        dbContext.Warehouses.AddRange(warehouseOne, warehouseTwo);
        dbContext.Zones.AddRange(zoneOne, zoneTwo);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return new SeededTopology(warehouseOne, warehouseTwo, zoneOne, zoneTwo);
    }

    private static async Task AddStorageLocationAsync(
        TestWmsDbContext testDbContext,
        Warehouse warehouse,
        Zone zone,
        string code,
        string name,
        bool isActive = true,
        bool isTypeActive = true,
        bool isStatusActive = true)
    {
        StorageLocationType type = StorageLocationType.CreateSystem(
            $"{code}-TYPE",
            $"{name} Type",
            description: null,
            sortOrder: 100);

        StorageLocationStatus status = StorageLocationStatus.CreateSystem(
            $"{code}-STATUS",
            $"{name} Status",
            description: null,
            sortOrder: 100);

        if (!isTypeActive)
        {
            type.Deactivate();
        }

        if (!isStatusActive)
        {
            status.Deactivate();
        }

        testDbContext.DbContext.StorageLocationTypes.Add(type);
        testDbContext.DbContext.StorageLocationStatuses.Add(status);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = StorageLocation.Create(
            warehouse.Id,
            zone.Id,
            type.Id,
            status.Id,
            code,
            name,
            description: null,
            isPickable: true,
            out StorageLocation? storageLocation);

        Assert.True(result.IsValid);
        Assert.NotNull(storageLocation);

        if (!isActive)
        {
            storageLocation.Deactivate();
        }

        storageLocation.ClearDomainEvents();
        testDbContext.DbContext.StorageLocations.Add(storageLocation);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static Warehouse CreateWarehouse(string code, string name)
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

    private static Zone CreateZone(Guid warehouseId, string code, string name)
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

    private sealed record SeededTopology(
        Warehouse WarehouseOne,
        Warehouse WarehouseTwo,
        Zone ZoneOne,
        Zone ZoneTwo);
}
