using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;
using Myrmex.Modules.Wms.Topology.Features.Warehouses;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Topology.Features.Warehouses;

public sealed class DeactivateWarehouseHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenWarehouseDoesNotExist_ReturnsNotFoundServiceResult()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        DeactivateWarehouse.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        DeactivateWarehouse.Command command = new(Guid.NewGuid());

        ServiceResult<WarehouseDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);

        Assert.Equal(ServiceErrorType.NotFound, result.Error.Type);
        Assert.Equal("Warehouse.NotFound", result.Error.Code);
        Assert.Equal("Warehouse was not found.", result.Error.Message);
        Assert.Null(result.Error.Property);

        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenWarehouseIsActive_DeactivatesWarehouseAndReturnsDetails()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        Warehouse warehouse = CreateWarehouse();

        testDbContext.DbContext.Warehouses.Add(warehouse);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        DeactivateWarehouse.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        DeactivateWarehouse.Command command = new(warehouse.Id);

        ServiceResult<WarehouseDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);

        WarehouseDetails details = result.Value;

        Assert.Equal(warehouse.Id, details.Id);
        Assert.Equal("MAIN", details.Code);
        Assert.Equal("Main Warehouse", details.Name);
        Assert.False(details.IsActive);

        Warehouse persistedWarehouse = await testDbContext.DbContext.Warehouses.SingleAsync(
            TestContext.Current.CancellationToken);

        Assert.False(persistedWarehouse.IsActive);
        Assert.NotEmpty(domainEventDispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenWarehouseIsAlreadyInactive_ReturnsSuccessWithoutDispatchingNewDomainEvent()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        Warehouse warehouse = CreateWarehouse();
        warehouse.Deactivate();
        warehouse.ClearDomainEvents();

        testDbContext.DbContext.Warehouses.Add(warehouse);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        DeactivateWarehouse.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        DeactivateWarehouse.Command command = new(warehouse.Id);

        ServiceResult<WarehouseDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);

        WarehouseDetails details = result.Value;

        Assert.False(details.IsActive);

        Warehouse persistedWarehouse = await testDbContext.DbContext.Warehouses.SingleAsync(
            TestContext.Current.CancellationToken);

        Assert.False(persistedWarehouse.IsActive);
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