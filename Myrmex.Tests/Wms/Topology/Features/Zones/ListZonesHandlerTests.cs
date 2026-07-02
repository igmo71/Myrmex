using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;
using Myrmex.Modules.Wms.Topology.Domain.Zones;
using Myrmex.Modules.Wms.Topology.Features.Zones;
using Myrmex.Shared.Common;
using Myrmex.Tests.Wms.Topology.Testing;
using System.Data.SqlTypes;

namespace Myrmex.Tests.Wms.Topology.Features.Zones;

public sealed class ListZonesHandlerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HandleAsync_WhenNameValuesMatch_OrdersByIdAcrossPages(bool sortDescending)
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        string marker = Guid.NewGuid().ToString("N")[..8];
        Warehouse warehouse = CreateWarehouse($"WH-{marker}", $"Warehouse {marker}");
        Zone[] zones =
        [
            CreateZone(warehouse.Id, $"ZONE-{marker}-A", $"Matching {marker}"),
            CreateZone(warehouse.Id, $"ZONE-{marker}-B", $"Matching {marker}"),
            CreateZone(warehouse.Id, $"ZONE-{marker}-C", $"Matching {marker}")
        ];

        testDbContext.DbContext.Warehouses.Add(warehouse);
        testDbContext.DbContext.Zones.AddRange(zones);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        ListZones.Handler handler = new(testDbContext.DbContext);

        ListResult<ZoneDetails> firstPage = await GetPageAsync(handler, warehouse.Id, marker, sortDescending, skip: 0);
        ListResult<ZoneDetails> secondPage = await GetPageAsync(handler, warehouse.Id, marker, sortDescending, skip: 2);

        Guid[] expectedIds = zones
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

    private static async Task<ListResult<ZoneDetails>> GetPageAsync(
        ListZones.Handler handler,
        Guid warehouseId,
        string marker,
        bool sortDescending,
        int skip)
    {
        ServiceResult<ListResult<ZoneDetails>> result = await handler.HandleAsync(
            new ListZones.Query(warehouseId)
            {
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
}
