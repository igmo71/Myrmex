using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;
using Myrmex.Modules.Wms.Topology.Features.Warehouses;
using Myrmex.Shared.Wms.Topology;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Topology.Features.Warehouses;

public sealed class DeactivateWarehouseHandlerTests
{
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

    [Fact]
    public async Task HandleAsync_WhenLinkedWarehouseIsActive_RejectsSourceOwnedTransition()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();
        Warehouse warehouse = CreateLinkedWarehouse(isDeletionMarked: false);
        testDbContext.DbContext.Warehouses.Add(warehouse);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        DeactivateWarehouse.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        ServiceResult<WarehouseDetails> result = await handler.HandleAsync(
            new DeactivateWarehouse.Command(warehouse.Id),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorType.Invalid, result.Error.Type);
        Assert.True(warehouse.IsActive);
        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenLinkedWarehouseIsAlreadyInactive_ReturnsNoOp()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();
        Warehouse warehouse = CreateLinkedWarehouse(isDeletionMarked: true);
        DateTimeOffset? updatedAtUtc = warehouse.UpdatedAtUtc;
        testDbContext.DbContext.Warehouses.Add(warehouse);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        DeactivateWarehouse.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        ServiceResult<WarehouseDetails> result = await handler.HandleAsync(
            new DeactivateWarehouse.Command(warehouse.Id),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsActive);
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
