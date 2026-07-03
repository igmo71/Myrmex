using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;
using Myrmex.Modules.Wms.Topology.Domain.Zones;
using Myrmex.Modules.Wms.Topology.Features.StorageLocations;
using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Topology;
using Myrmex.Tests.Wms.Topology.Testing;
using System.Data.SqlTypes;

namespace Myrmex.Tests.Wms.Topology.Features.StorageLocations;

public sealed class ListStorageLocationsHandlerTests
{
    [Fact]
    public async Task HandleAsync_AppliesAllFiltersBeforeCountAndPaging()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        string marker = Guid.NewGuid().ToString("N")[..8];
        Warehouse warehouse = CreateWarehouse($"WH-{marker}-A", $"Warehouse {marker} A");
        Warehouse otherWarehouse = CreateWarehouse($"WH-{marker}-B", $"Warehouse {marker} B");
        Zone zone = CreateZone(warehouse.Id, $"ZONE-{marker}-A", $"Zone {marker} A");
        Zone otherZone = CreateZone(otherWarehouse.Id, $"ZONE-{marker}-B", $"Zone {marker} B");
        StorageLocationType selectedType = StorageLocationType.CreateSystem(
            $"TYPE-{marker}-A", $"Type {marker} A", null, 10);
        StorageLocationType otherType = StorageLocationType.CreateSystem(
            $"TYPE-{marker}-B", $"Type {marker} B", null, 20);
        StorageLocationStatus selectedStatus = StorageLocationStatus.CreateSystem(
            $"STATUS-{marker}-A", $"Status {marker} A", null, 10);
        StorageLocationStatus otherStatus = StorageLocationStatus.CreateSystem(
            $"STATUS-{marker}-B", $"Status {marker} B", null, 20);

        string nonMatchingSuffix = Guid.NewGuid().ToString("N")[..8];

        StorageLocation[] locations =
        [
            CreateStorageLocation(warehouse.Id, zone.Id, selectedType.Id, selectedStatus.Id,
                $"LOC-{marker}-01", $"Match {marker} One"),
            CreateStorageLocation(warehouse.Id, zone.Id, selectedType.Id, selectedStatus.Id,
                $"LOC-{marker}-02", $"Match {marker} Two"),
            CreateStorageLocation(warehouse.Id, zone.Id, otherType.Id, selectedStatus.Id,
                $"LOC-{marker}-03", $"Match {marker} Wrong Type"),
            CreateStorageLocation(warehouse.Id, zone.Id, selectedType.Id, otherStatus.Id,
                $"LOC-{marker}-04", $"Match {marker} Wrong Status"),
            CreateStorageLocation(warehouse.Id, zone.Id, selectedType.Id, selectedStatus.Id,
                $"LOC-NOSEARCH-{nonMatchingSuffix}", "No search match"),
            CreateStorageLocation(otherWarehouse.Id, otherZone.Id, selectedType.Id, selectedStatus.Id,
                $"LOC-{marker}-05", $"Match {marker} Other Warehouse")
        ];

        StorageLocation inactive = CreateStorageLocation(
            warehouse.Id,
            zone.Id,
            selectedType.Id,
            selectedStatus.Id,
            $"LOC-{marker}-06",
            $"Match {marker} Inactive");
        inactive.Deactivate();

        testDbContext.DbContext.Warehouses.AddRange(warehouse, otherWarehouse);
        testDbContext.DbContext.Zones.AddRange(zone, otherZone);
        testDbContext.DbContext.StorageLocationTypes.AddRange(selectedType, otherType);
        testDbContext.DbContext.StorageLocationStatuses.AddRange(selectedStatus, otherStatus);
        testDbContext.DbContext.StorageLocations.AddRange(locations.Append(inactive));
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        ListStorageLocations.Handler handler = new(testDbContext.DbContext);
        ServiceResult<ListResult<StorageLocationDetails>> result = await handler.HandleAsync(
            new ListStorageLocations.Query
            {
                WarehouseId = warehouse.Id,
                ZoneId = zone.Id,
                StorageLocationTypeId = selectedType.Id,
                StorageLocationStatusId = selectedStatus.Id,
                SearchText = marker,
                IncludeInactive = false,
                Skip = 1,
                Take = 1,
                SortBy = StorageLocationSortBy.Code
            },
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Single(result.Value.Items);
        Assert.All(result.Value.Items, item =>
        {
            Assert.Equal(warehouse.Id, item.WarehouseId);
            Assert.Equal(zone.Id, item.ZoneId);
            Assert.Equal(selectedType.Id, item.StorageLocationTypeId);
            Assert.Equal(selectedStatus.Id, item.StorageLocationStatusId);
            Assert.True(item.IsActive);
        });
    }

    [Fact]
    public async Task HandleAsync_WhenTypeAndStatusIdsDoNotMatch_ReturnsEmptyPage()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        ListStorageLocations.Handler handler = new(testDbContext.DbContext);

        ServiceResult<ListResult<StorageLocationDetails>> result = await handler.HandleAsync(
            new ListStorageLocations.Query
            {
                StorageLocationTypeId = Guid.NewGuid(),
                StorageLocationStatusId = Guid.NewGuid()
            },
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalCount);
    }

    [Fact]
    public async Task HandleAsync_WhenZoneBelongsToAnotherWarehouse_PreservesConflict()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        string marker = Guid.NewGuid().ToString("N")[..8];
        Warehouse warehouse = CreateWarehouse($"WH-{marker}-A", $"Warehouse {marker} A");
        Warehouse otherWarehouse = CreateWarehouse($"WH-{marker}-B", $"Warehouse {marker} B");
        Zone otherZone = CreateZone(otherWarehouse.Id, $"ZONE-{marker}", $"Zone {marker}");
        testDbContext.DbContext.Warehouses.AddRange(warehouse, otherWarehouse);
        testDbContext.DbContext.Zones.Add(otherZone);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        ListStorageLocations.Handler handler = new(testDbContext.DbContext);

        ServiceResult<ListResult<StorageLocationDetails>> result = await handler.HandleAsync(
            new ListStorageLocations.Query
            {
                WarehouseId = warehouse.Id,
                ZoneId = otherZone.Id
            },
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorType.Conflict, result.Error.Type);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HandleAsync_WhenNameValuesMatch_OrdersByIdAcrossPages(bool sortDescending)
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        string marker = Guid.NewGuid().ToString("N")[..8];
        Warehouse warehouse = CreateWarehouse($"WH-{marker}", $"Warehouse {marker}");
        Zone zone = CreateZone(warehouse.Id, $"ZONE-{marker}", $"Zone {marker}");
        StorageLocationType type = StorageLocationType.CreateSystem(
            $"TYPE-{marker}", $"Type {marker}", description: null, sortOrder: 100);
        StorageLocationStatus status = StorageLocationStatus.CreateSystem(
            $"STATUS-{marker}", $"Status {marker}", description: null, sortOrder: 100);
        StorageLocation[] storageLocations =
        [
            CreateStorageLocation(warehouse.Id, zone.Id, type.Id, status.Id, $"LOC-{marker}-A", $"Matching {marker}"),
            CreateStorageLocation(warehouse.Id, zone.Id, type.Id, status.Id, $"LOC-{marker}-B", $"Matching {marker}"),
            CreateStorageLocation(warehouse.Id, zone.Id, type.Id, status.Id, $"LOC-{marker}-C", $"Matching {marker}")
        ];

        testDbContext.DbContext.Warehouses.Add(warehouse);
        testDbContext.DbContext.Zones.Add(zone);
        testDbContext.DbContext.StorageLocationTypes.Add(type);
        testDbContext.DbContext.StorageLocationStatuses.Add(status);
        testDbContext.DbContext.StorageLocations.AddRange(storageLocations);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        ListStorageLocations.Handler handler = new(testDbContext.DbContext);

        ListResult<StorageLocationDetails> firstPage = await GetPageAsync(
            handler, warehouse.Id, zone.Id, marker, sortDescending, skip: 0);
        ListResult<StorageLocationDetails> secondPage = await GetPageAsync(
            handler, warehouse.Id, zone.Id, marker, sortDescending, skip: 2);

        Guid[] expectedIds = storageLocations
            .OrderBy(x => new SqlGuid(x.Id))
            .Select(x => x.Id)
            .ToArray();
        Guid[] actualIds = firstPage.Items
            .Concat(secondPage.Items)
            .Select(x => x.Id)
            .ToArray();

        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(3, secondPage.TotalCount);
        Assert.Equal(expectedIds, actualIds);
    }

    private static async Task<ListResult<StorageLocationDetails>> GetPageAsync(
        ListStorageLocations.Handler handler,
        Guid warehouseId,
        Guid zoneId,
        string marker,
        bool sortDescending,
        int skip)
    {
        ServiceResult<ListResult<StorageLocationDetails>> result = await handler.HandleAsync(
            new ListStorageLocations.Query
            {
                WarehouseId = warehouseId,
                ZoneId = zoneId,
                SearchText = marker,
                SortBy = "name",
                SortDescending = sortDescending,
                Skip = skip,
                Take = 2
            },
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private static Warehouse CreateWarehouse(string code, string name)
    {
        var result = Warehouse.Create(code, name, description: null, out Warehouse? warehouse);

        Assert.True(result.IsValid);
        Assert.NotNull(warehouse);
        warehouse.ClearDomainEvents();

        return warehouse;
    }

    private static Zone CreateZone(Guid warehouseId, string code, string name)
    {
        var result = Zone.Create(warehouseId, code, name, description: null, out Zone? zone);

        Assert.True(result.IsValid);
        Assert.NotNull(zone);
        zone.ClearDomainEvents();

        return zone;
    }

    private static StorageLocation CreateStorageLocation(
        Guid warehouseId,
        Guid zoneId,
        Guid typeId,
        Guid statusId,
        string code,
        string name)
    {
        var result = StorageLocation.Create(
            warehouseId,
            zoneId,
            typeId,
            statusId,
            code,
            name,
            description: null,
            isPickable: true,
            out StorageLocation? storageLocation);

        Assert.True(result.IsValid);
        Assert.NotNull(storageLocation);
        storageLocation.ClearDomainEvents();

        return storageLocation;
    }
}
