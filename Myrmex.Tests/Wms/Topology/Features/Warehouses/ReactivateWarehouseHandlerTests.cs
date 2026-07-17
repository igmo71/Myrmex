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

    [Fact]
    public async Task HandleAsync_WhenLinkedWarehouseIsInactive_RejectsSourceOwnedTransition()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();
        Warehouse warehouse = CreateLinkedWarehouse(isDeletionMarked: true);
        testDbContext.DbContext.Warehouses.Add(warehouse);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        ReactivateWarehouse.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        ServiceResult<WarehouseDetails> result = await handler.HandleAsync(
            new ReactivateWarehouse.Command(warehouse.Id),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorType.Invalid, result.Error.Type);
        Assert.False(warehouse.IsActive);
        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenLinkedWarehouseIsAlreadyActive_ReturnsNoOp()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();
        Warehouse warehouse = CreateLinkedWarehouse(isDeletionMarked: false);
        DateTimeOffset? updatedAtUtc = warehouse.UpdatedAtUtc;
        testDbContext.DbContext.Warehouses.Add(warehouse);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        ReactivateWarehouse.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        ServiceResult<WarehouseDetails> result = await handler.HandleAsync(
            new ReactivateWarehouse.Command(warehouse.Id),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsActive);
        Assert.Equal(updatedAtUtc, warehouse.UpdatedAtUtc);
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

    private static Warehouse CreateLinkedWarehouse(bool isDeletionMarked)
    {
        Warehouse warehouse = CreateWarehouse();
        var result = warehouse.ApplyImport(
            Guid.NewGuid(),
            [1],
            warehouse.Code,
            warehouse.Name,
            isDeletionMarked,
            importedAtUtc: DateTimeOffset.Parse("2026-07-17T12:00:00Z"));
        Assert.True(result.IsValid);
        warehouse.ClearDomainEvents();
        return warehouse;
    }
}
