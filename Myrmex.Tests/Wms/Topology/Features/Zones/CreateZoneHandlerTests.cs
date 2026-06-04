using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;
using Myrmex.Modules.Wms.Topology.Features.Zones;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Topology.Features.Zones;

public sealed class CreateZoneHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenCommandIsInvalid_ReturnsInvalidServiceResult()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        CreateZone.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        CreateZone.Command command = new(
            WarehouseId: Guid.Empty,
            Code: "",
            Name: "",
            Description: null);

        ServiceResult<ZoneDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);

        Assert.Equal(ServiceErrorType.Invalid, result.Error.Type);
        Assert.Equal("Validation.Invalid", result.Error.Code);

        Assert.Contains(result.Error.DetailList, error =>
            error.Code == "Zone.WarehouseIdRequired" &&
            error.Field == "warehouseId");

        Assert.Contains(result.Error.DetailList, error =>
            error.Code == "Zone.CodeRequired" &&
            error.Field == "code");

        Assert.Contains(result.Error.DetailList, error =>
            error.Code == "Zone.NameRequired" &&
            error.Field == "name");

        Assert.Empty(await testDbContext.DbContext.Zones.ToListAsync(
            TestContext.Current.CancellationToken));

        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenWarehouseDoesNotExist_ReturnsWarehouseNotFound()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        CreateZone.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        CreateZone.Command command = new(
            WarehouseId: Guid.NewGuid(),
            Code: "ZONE-A",
            Name: "Zone A",
            Description: null);

        ServiceResult<ZoneDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);

        Assert.Equal(ServiceErrorType.NotFound, result.Error.Type);
        Assert.Equal("Warehouse.NotFound", result.Error.Code);
        Assert.Equal("Warehouse was not found.", result.Error.Message);
        Assert.Equal("warehouseId", result.Error.Field);

        Assert.Empty(await testDbContext.DbContext.Zones.ToListAsync(
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task HandleAsync_WhenCodeAlreadyExistsInWarehouse_ReturnsConflictServiceResult()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        Warehouse warehouse = CreateWarehouse();
        testDbContext.DbContext.Warehouses.Add(warehouse);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        CreateZone.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        CreateZone.Command firstCommand = new(
            WarehouseId: warehouse.Id,
            Code: "ZONE-A",
            Name: "Zone A",
            Description: null);

        ServiceResult<ZoneDetails> firstResult = await handler.HandleAsync(
            firstCommand,
            TestContext.Current.CancellationToken);

        Assert.True(firstResult.IsSuccess);

        CreateZone.Command duplicateCommand = new(
            WarehouseId: warehouse.Id,
            Code: " zone-a ",
            Name: "Another Zone",
            Description: null);

        ServiceResult<ZoneDetails> result = await handler.HandleAsync(
            duplicateCommand,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);

        Assert.Equal(ServiceErrorType.Conflict, result.Error.Type);
        Assert.Equal("Zone.CodeAlreadyExists", result.Error.Code);
        Assert.Equal("Zone with the same code already exists in this warehouse.", result.Error.Message);
        Assert.Equal("code", result.Error.Field);

        int zoneCount = await testDbContext.DbContext.Zones.CountAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(1, zoneCount);
    }

    [Fact]
    public async Task HandleAsync_WhenCommandIsValid_CreatesZoneAndReturnsDetails()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        Warehouse warehouse = CreateWarehouse();
        testDbContext.DbContext.Warehouses.Add(warehouse);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        CreateZone.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        CreateZone.Command command = new(
            WarehouseId: warehouse.Id,
            Code: " zone-a ",
            Name: " Zone A ",
            Description: " Picking zone ");

        ServiceResult<ZoneDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);

        ZoneDetails details = result.Value;

        Assert.NotEqual(Guid.Empty, details.Id);
        Assert.Equal(warehouse.Id, details.WarehouseId);
        Assert.Equal("ZONE-A", details.Code);
        Assert.Equal("Zone A", details.Name);
        Assert.Equal("Picking zone", details.Description);
        Assert.True(details.IsActive);

        var zone = await testDbContext.DbContext.Zones.SingleAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(details.Id, zone.Id);
        Assert.Equal(warehouse.Id, zone.WarehouseId);
        Assert.Equal("ZONE-A", zone.Code);
        Assert.Equal("Zone A", zone.Name);
        Assert.Equal("Picking zone", zone.Description);

        Assert.NotEmpty(domainEventDispatcher.DispatchedEvents);
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