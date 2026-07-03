using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;
using Myrmex.Modules.Wms.Topology.Features.Warehouses;
using Myrmex.Shared.Wms.Topology;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Topology.Features.Warehouses;

public sealed class ReactivateWarehouseHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenWarehouseIsInactive_ReactivatesWarehouseAndReturnsDetails()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        Warehouse warehouse = CreateWarehouse();
        warehouse.Deactivate();
        warehouse.ClearDomainEvents();

        testDbContext.DbContext.Warehouses.Add(warehouse);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        ReactivateWarehouse.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        ReactivateWarehouse.Command command = new(warehouse.Id);

        ServiceResult<WarehouseDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);

        WarehouseDetails details = result.Value;

        Assert.Equal(warehouse.Id, details.Id);
        Assert.Equal("MAIN", details.Code);
        Assert.Equal("Main Warehouse", details.Name);
        Assert.True(details.IsActive);

        Warehouse persistedWarehouse = await testDbContext.DbContext.Warehouses.SingleAsync(
            TestContext.Current.CancellationToken);

        Assert.True(persistedWarehouse.IsActive);
        Assert.NotEmpty(domainEventDispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenWarehouseIsAlreadyActive_ReturnsSuccessWithoutDispatchingNewDomainEvent()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        Warehouse warehouse = CreateWarehouse();
        warehouse.ClearDomainEvents();

        testDbContext.DbContext.Warehouses.Add(warehouse);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        ReactivateWarehouse.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        ReactivateWarehouse.Command command = new(warehouse.Id);

        ServiceResult<WarehouseDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);

        WarehouseDetails details = result.Value;

        Assert.True(details.IsActive);

        Warehouse persistedWarehouse = await testDbContext.DbContext.Warehouses.SingleAsync(
            TestContext.Current.CancellationToken);

        Assert.True(persistedWarehouse.IsActive);
        Assert.Empty(domainEventDispatcher.DispatchedEvents);
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

        return warehouse;
    }
}
