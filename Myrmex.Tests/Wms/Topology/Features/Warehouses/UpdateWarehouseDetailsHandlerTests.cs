using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;
using Myrmex.Modules.Wms.Topology.Features.Warehouses;
using Myrmex.Shared.Wms.Topology;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Topology.Features.Warehouses;

public sealed class UpdateWarehouseDetailsHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenCommandIsValid_UpdatesWarehouseAndReturnsDetails()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        Warehouse warehouse = CreateWarehouse();

        testDbContext.DbContext.Warehouses.Add(warehouse);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        UpdateWarehouseDetails.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        UpdateWarehouseDetails.Command command = new(
            WarehouseId: warehouse.Id,
            Name: " Updated Warehouse ",
            Description: " Updated description ");

        ServiceResult<WarehouseDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);

        WarehouseDetails details = result.Value;

        Assert.Equal(warehouse.Id, details.Id);
        Assert.Equal("MAIN", details.Code);
        Assert.Equal("Updated Warehouse", details.Name);
        Assert.Equal("Updated description", details.Description);
        Assert.True(details.IsActive);

        Warehouse persistedWarehouse = await testDbContext.DbContext.Warehouses.SingleAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal("Updated Warehouse", persistedWarehouse.Name);
        Assert.Equal("Updated description", persistedWarehouse.Description);

        Assert.NotEmpty(domainEventDispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenLinkedNameChanges_RejectsTheActualSourceOwnedChange()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();
        Warehouse warehouse = CreateLinkedWarehouse();
        testDbContext.DbContext.Warehouses.Add(warehouse);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        UpdateWarehouseDetails.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        ServiceResult<WarehouseDetails> result = await handler.HandleAsync(
            new UpdateWarehouseDetails.Command(
                warehouse.Id,
                Name: "Changed by WMS",
                Description: "Local description"),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorType.Invalid, result.Error.Type);
        Assert.Contains(result.Error.DetailList, error =>
            error.Code == "IncorrectState-Warehouse-Name");
        Assert.Equal("Main Warehouse", warehouse.Name);
        Assert.Null(warehouse.Description);
        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenLinkedNameIsUnchanged_AllowsDescriptionChange()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();
        Warehouse warehouse = CreateLinkedWarehouse();
        testDbContext.DbContext.Warehouses.Add(warehouse);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        UpdateWarehouseDetails.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        ServiceResult<WarehouseDetails> result = await handler.HandleAsync(
            new UpdateWarehouseDetails.Command(
                warehouse.Id,
                Name: " Main Warehouse ",
                Description: " Local description "),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("Main Warehouse", result.Value.Name);
        Assert.Equal("Local description", result.Value.Description);
        Assert.IsType<WarehouseDetailsUpdatedDomainEvent>(
            Assert.Single(domainEventDispatcher.DispatchedEvents));
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

    private static Warehouse CreateLinkedWarehouse()
    {
        Warehouse warehouse = CreateWarehouse();
        var result = warehouse.ApplyImport(
            Guid.NewGuid(),
            [1],
            warehouse.Code,
            warehouse.Name,
            isDeletionMarked: false,
            importedAtUtc: DateTimeOffset.Parse("2026-07-17T12:00:00Z"));
        Assert.True(result.IsValid);
        warehouse.ClearDomainEvents();
        return warehouse;
    }
}
