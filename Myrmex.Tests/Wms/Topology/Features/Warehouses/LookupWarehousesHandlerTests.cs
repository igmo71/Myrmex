using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;
using Myrmex.Modules.Wms.Topology.Features.Warehouses;
using Myrmex.Shared.Wms.Topology;
using Myrmex.Tests.Wms.Topology.Testing;
using System.Data.SqlTypes;

namespace Myrmex.Tests.Wms.Topology.Features.Warehouses;

public sealed class LookupWarehousesHandlerTests
{
    [Theory]
    [InlineData("CODE-NEEDLE")]
    [InlineData("Name Needle")]
    [InlineData("Description Needle")]
    public async Task HandleAsync_SearchesCodeNameAndDescription(string searchText)
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        string marker = Guid.NewGuid().ToString("N")[..8];
        Warehouse codeMatch = CreateWarehouse($"CODE-NEEDLE-{marker}", "Other", null);
        Warehouse nameMatch = CreateWarehouse($"NAME-{marker}", $"Name Needle {marker}", null);
        Warehouse descriptionMatch = CreateWarehouse(
            $"DESC-{marker}",
            "Other Description",
            $"Description Needle {marker}");
        testDbContext.DbContext.Warehouses.AddRange(codeMatch, nameMatch, descriptionMatch);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        LookupWarehouses.Handler handler = new(testDbContext.DbContext);

        ServiceResult<IReadOnlyList<WarehouseLookupItem>> result = await handler.HandleAsync(
            new LookupWarehouses.Query
            {
                SearchText = searchText,
                SelectableOnly = true
            },
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
    }

    [Fact]
    public async Task HandleAsync_SelectableOnlyReturnsActiveWarehouses()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        string marker = Guid.NewGuid().ToString("N")[..8];
        Warehouse active = CreateWarehouse($"ACTIVE-{marker}", "Active", null);
        Warehouse inactive = CreateWarehouse($"INACTIVE-{marker}", "Inactive", null);
        inactive.Deactivate();
        testDbContext.DbContext.Warehouses.AddRange(active, inactive);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        LookupWarehouses.Handler handler = new(testDbContext.DbContext);

        ServiceResult<IReadOnlyList<WarehouseLookupItem>> selectable = await handler.HandleAsync(
            new LookupWarehouses.Query { SearchText = marker, SelectableOnly = true },
            TestContext.Current.CancellationToken);
        ServiceResult<IReadOnlyList<WarehouseLookupItem>> all = await handler.HandleAsync(
            new LookupWarehouses.Query { SearchText = marker, SelectableOnly = false },
            TestContext.Current.CancellationToken);

        Assert.True(selectable.IsSuccess);
        Assert.Equal([active.Id], selectable.Value.Select(x => x.Id).ToArray());
        Assert.True(all.IsSuccess);
        Assert.Equal(2, all.Value.Count);
        Assert.Contains(all.Value, x => x.Id == inactive.Id && !x.IsActive);
    }

    [Fact]
    public async Task HandleAsync_DefaultAndMaximumTakeAreTwenty()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        string marker = Guid.NewGuid().ToString("N")[..8];
        for (int index = 1; index <= 25; index++)
        {
            testDbContext.DbContext.Warehouses.Add(CreateWarehouse(
                $"WH-{marker}-{index:000}",
                $"Warehouse {marker} {index:000}",
                null));
        }

        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        LookupWarehouses.Handler handler = new(testDbContext.DbContext);

        ServiceResult<IReadOnlyList<WarehouseLookupItem>> defaultResult = await handler.HandleAsync(
            new LookupWarehouses.Query { SearchText = marker },
            TestContext.Current.CancellationToken);
        ServiceResult<IReadOnlyList<WarehouseLookupItem>> maximumResult = await handler.HandleAsync(
            new LookupWarehouses.Query { SearchText = marker, Take = 1_000 },
            TestContext.Current.CancellationToken);

        Assert.Equal(20, defaultResult.Value.Count);
        Assert.Equal(20, maximumResult.Value.Count);
    }

    [Fact]
    public async Task HandleAsync_OrdersByNameThenCodeThenId()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        string marker = Guid.NewGuid().ToString("N")[..8];
        Warehouse[] warehouses =
        [
            CreateWarehouse($"B-{marker}", $"Same {marker}", null),
            CreateWarehouse($"A-{marker}", $"Same {marker}", null),
            CreateWarehouse($"C-{marker}", $"After {marker}", null)
        ];
        testDbContext.DbContext.Warehouses.AddRange(warehouses);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        LookupWarehouses.Handler handler = new(testDbContext.DbContext);

        ServiceResult<IReadOnlyList<WarehouseLookupItem>> result = await handler.HandleAsync(
            new LookupWarehouses.Query { SearchText = marker },
            TestContext.Current.CancellationToken);

        Guid[] expected = warehouses
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Code)
            .ThenBy(x => new SqlGuid(x.Id))
            .Select(x => x.Id)
            .ToArray();
        Assert.Equal(expected, result.Value.Select(x => x.Id).ToArray());
    }

    [Fact]
    public async Task HandleAsync_PropagatesCancellation()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        LookupWarehouses.Handler handler = new(testDbContext.DbContext);
        using CancellationTokenSource cancellationTokenSource = new();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => handler.HandleAsync(
            new LookupWarehouses.Query(),
            cancellationTokenSource.Token));
    }

    private static Warehouse CreateWarehouse(string code, string name, string? description)
    {
        var result = Warehouse.Create(code, name, description, out Warehouse? warehouse);
        Assert.True(result.IsValid);
        Assert.NotNull(warehouse);
        warehouse.ClearDomainEvents();
        return warehouse;
    }
}
