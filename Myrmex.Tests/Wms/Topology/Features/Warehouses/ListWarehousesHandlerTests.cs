using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;
using Myrmex.Modules.Wms.Topology.Features.Warehouses;
using Myrmex.Shared.Wms.Topology;
using Myrmex.Shared.Common;
using Myrmex.Tests.Wms.Topology.Testing;
using System.Data.SqlTypes;

namespace Myrmex.Tests.Wms.Topology.Features.Warehouses;

public sealed class ListWarehousesHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenSortIsNotSpecified_OrdersByNameThenId()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        string marker = Guid.NewGuid().ToString("N");
        Warehouse beta = CreateWarehouse($"A-{marker[..8]}", $"{marker} Beta");
        Warehouse alphaOne = CreateWarehouse($"Z-{marker[..8]}", $"{marker} Alpha");
        Warehouse alphaTwo = CreateWarehouse($"M-{marker[..8]}", $"{marker} Alpha");
        Warehouse[] warehouses = [beta, alphaOne, alphaTwo];

        testDbContext.DbContext.Warehouses.AddRange(warehouses);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        ListWarehouses.Handler handler = new(testDbContext.DbContext);

        ServiceResult<ListResult<WarehouseDetails>> result = await handler.HandleAsync(
            new ListWarehouses.Query { SearchText = marker },
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Guid[] expectedIds = warehouses
            .OrderBy(x => x.Name)
            .ThenBy(x => new SqlGuid(x.Id))
            .Select(x => x.Id)
            .ToArray();
        Assert.Equal(expectedIds, result.Value.Items.Select(x => x.Id).ToArray());
    }

    private static Warehouse CreateWarehouse(string code, string name)
    {
        var validation = Warehouse.Create(code, name, description: null, out Warehouse? warehouse);

        Assert.True(validation.IsValid);
        Assert.NotNull(warehouse);
        warehouse.ClearDomainEvents();

        return warehouse;
    }
}
