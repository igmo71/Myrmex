using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;
using Myrmex.Modules.Wms.Topology.Domain.Zones;
using Myrmex.Modules.Wms.Topology.Features.StorageLocations;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Topology.Features.StorageLocations;

public sealed class CreateStorageLocationHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenCommandIsInvalid_ReturnsInvalidServiceResult()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        CreateStorageLocation.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        CreateStorageLocation.Command command = new(
            WarehouseId: Guid.Empty,
            ZoneId: Guid.Empty,
            StorageLocationTypeId: Guid.Empty,
            StorageLocationStatusId: Guid.Empty,
            Code: "",
            Name: "",
            Description: null,
            IsPickable: true);

        ServiceResult<StorageLocationDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);

        Assert.Equal(ServiceErrorType.Invalid, result.Error.Type);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Equal("One or more validation errors occurred.", result.Error.Message);

        Assert.Contains(result.Error.DetailList, error =>
            error.Code == "StorageLocation.WarehouseIdRequired" &&
            error.Property == "warehouseId");

        Assert.Contains(result.Error.DetailList, error =>
            error.Code == "StorageLocation.ZoneIdRequired" &&
            error.Property == "zoneId");

        Assert.Contains(result.Error.DetailList, error =>
            error.Code == "StorageLocation.TypeIdRequired" &&
            error.Property == "storageLocationTypeId");

        Assert.Contains(result.Error.DetailList, error =>
            error.Code == "StorageLocation.StatusIdRequired" &&
            error.Property == "storageLocationStatusId");

        Assert.Contains(result.Error.DetailList, error =>
            error.Code == "StorageLocation.CodeRequired" &&
            error.Property == "code");

        Assert.Contains(result.Error.DetailList, error =>
            error.Code == "StorageLocation.NameRequired" &&
            error.Property == "name");

        Assert.Empty(await testDbContext.DbContext.StorageLocations.ToListAsync(
            TestContext.Current.CancellationToken));

        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenWarehouseDoesNotExist_ReturnsWarehouseNotFound()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        StorageLocationType type = await GetStorageLocationTypeAsync(testDbContext.DbContext);
        StorageLocationStatus status = await GetStorageLocationStatusAsync(testDbContext.DbContext);

        CreateStorageLocation.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        CreateStorageLocation.Command command = new(
            WarehouseId: Guid.NewGuid(),
            ZoneId: Guid.NewGuid(),
            StorageLocationTypeId: type.Id,
            StorageLocationStatusId: status.Id,
            Code: "A-01-01",
            Name: "A-01-01",
            Description: null,
            IsPickable: true);

        ServiceResult<StorageLocationDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);

        Assert.Equal(ServiceErrorType.NotFound, result.Error.Type);
        Assert.Equal("Warehouse.NotFound", result.Error.Code);
        Assert.Equal("Warehouse was not found.", result.Error.Message);
        Assert.Equal("warehouseId", result.Error.Property);

        Assert.Empty(await testDbContext.DbContext.StorageLocations.ToListAsync(
            TestContext.Current.CancellationToken));

        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenZoneDoesNotExist_ReturnsZoneNotFound()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        Warehouse warehouse = CreateWarehouse();
        StorageLocationType type = await GetStorageLocationTypeAsync(testDbContext.DbContext);
        StorageLocationStatus status = await GetStorageLocationStatusAsync(testDbContext.DbContext);

        testDbContext.DbContext.Warehouses.Add(warehouse);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        CreateStorageLocation.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        CreateStorageLocation.Command command = new(
            WarehouseId: warehouse.Id,
            ZoneId: Guid.NewGuid(),
            StorageLocationTypeId: type.Id,
            StorageLocationStatusId: status.Id,
            Code: "A-01-01",
            Name: "A-01-01",
            Description: null,
            IsPickable: true);

        ServiceResult<StorageLocationDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);

        Assert.Equal(ServiceErrorType.NotFound, result.Error.Type);
        Assert.Equal("Zone.NotFound", result.Error.Code);
        Assert.Equal("Zone was not found.", result.Error.Message);
        Assert.Equal("zoneId", result.Error.Property);

        Assert.Empty(await testDbContext.DbContext.StorageLocations.ToListAsync(
            TestContext.Current.CancellationToken));

        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenZoneBelongsToAnotherWarehouse_ReturnsZoneWarehouseMismatch()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        Warehouse commandWarehouse = CreateWarehouse(code: "MAIN", name: "Main Warehouse");
        Warehouse zoneWarehouse = CreateWarehouse(code: "OTHER", name: "Other Warehouse");
        Zone zone = CreateZone(zoneWarehouse.Id);
        StorageLocationType type = await GetStorageLocationTypeAsync(testDbContext.DbContext);
        StorageLocationStatus status = await GetStorageLocationStatusAsync(testDbContext.DbContext);

        testDbContext.DbContext.Warehouses.AddRange(commandWarehouse, zoneWarehouse);
        testDbContext.DbContext.Zones.Add(zone);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        CreateStorageLocation.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        CreateStorageLocation.Command command = new(
            WarehouseId: commandWarehouse.Id,
            ZoneId: zone.Id,
            StorageLocationTypeId: type.Id,
            StorageLocationStatusId: status.Id,
            Code: "A-01-01",
            Name: "A-01-01",
            Description: null,
            IsPickable: true);

        ServiceResult<StorageLocationDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);

        Assert.Equal(ServiceErrorType.Failure, result.Error.Type);
        Assert.Equal("StorageLocation.ZoneWarehouseMismatch", result.Error.Code);
        Assert.Equal("Zone does not belong to the specified warehouse.", result.Error.Message);

        Assert.Empty(await testDbContext.DbContext.StorageLocations.ToListAsync(
            TestContext.Current.CancellationToken));

        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenTypeDoesNotExist_ReturnsTypeNotFound()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        Warehouse warehouse = CreateWarehouse();
        Zone zone = CreateZone(warehouse.Id);
        StorageLocationStatus status = await GetStorageLocationStatusAsync(testDbContext.DbContext);

        testDbContext.DbContext.Warehouses.Add(warehouse);
        testDbContext.DbContext.Zones.Add(zone);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        CreateStorageLocation.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        CreateStorageLocation.Command command = new(
            WarehouseId: warehouse.Id,
            ZoneId: zone.Id,
            StorageLocationTypeId: Guid.NewGuid(),
            StorageLocationStatusId: status.Id,
            Code: "A-01-01",
            Name: "A-01-01",
            Description: null,
            IsPickable: true);

        ServiceResult<StorageLocationDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);

        Assert.Equal(ServiceErrorType.NotFound, result.Error.Type);
        Assert.Equal("StorageLocationType.NotFound", result.Error.Code);
        Assert.Equal("Storage location type was not found.", result.Error.Message);
        Assert.Equal("storageLocationTypeId", result.Error.Property);

        Assert.Empty(await testDbContext.DbContext.StorageLocations.ToListAsync(
            TestContext.Current.CancellationToken));

        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenStatusDoesNotExist_ReturnsStatusNotFound()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        Warehouse warehouse = CreateWarehouse();
        Zone zone = CreateZone(warehouse.Id);
        StorageLocationType type = await GetStorageLocationTypeAsync(testDbContext.DbContext);

        testDbContext.DbContext.Warehouses.Add(warehouse);
        testDbContext.DbContext.Zones.Add(zone);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        CreateStorageLocation.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        CreateStorageLocation.Command command = new(
            WarehouseId: warehouse.Id,
            ZoneId: zone.Id,
            StorageLocationTypeId: type.Id,
            StorageLocationStatusId: Guid.NewGuid(),
            Code: "A-01-01",
            Name: "A-01-01",
            Description: null,
            IsPickable: true);

        ServiceResult<StorageLocationDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);

        Assert.Equal(ServiceErrorType.NotFound, result.Error.Type);
        Assert.Equal("StorageLocationStatus.NotFound", result.Error.Code);
        Assert.Equal("Storage location status was not found.", result.Error.Message);
        Assert.Equal("storageLocationStatusId", result.Error.Property);

        Assert.Empty(await testDbContext.DbContext.StorageLocations.ToListAsync(
            TestContext.Current.CancellationToken));

        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenCodeAlreadyExistsInWarehouse_ReturnsConflictServiceResult()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        Warehouse warehouse = CreateWarehouse();
        Zone zone = CreateZone(warehouse.Id);
        StorageLocationType type = await GetStorageLocationTypeAsync(testDbContext.DbContext);
        StorageLocationStatus status = await GetStorageLocationStatusAsync(testDbContext.DbContext);

        testDbContext.DbContext.Warehouses.Add(warehouse);
        testDbContext.DbContext.Zones.Add(zone);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        CreateStorageLocation.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        CreateStorageLocation.Command firstCommand = new(
            WarehouseId: warehouse.Id,
            ZoneId: zone.Id,
            StorageLocationTypeId: type.Id,
            StorageLocationStatusId: status.Id,
            Code: "A-01-01",
            Name: "A-01-01",
            Description: null,
            IsPickable: true);

        ServiceResult<StorageLocationDetails> firstResult = await handler.HandleAsync(
            firstCommand,
            TestContext.Current.CancellationToken);

        Assert.True(firstResult.IsSuccess);

        CreateStorageLocation.Command duplicateCommand = new(
            WarehouseId: warehouse.Id,
            ZoneId: zone.Id,
            StorageLocationTypeId: type.Id,
            StorageLocationStatusId: status.Id,
            Code: " a-01-01 ",
            Name: "Duplicate location",
            Description: null,
            IsPickable: false);

        ServiceResult<StorageLocationDetails> result = await handler.HandleAsync(
            duplicateCommand,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);

        Assert.Equal(ServiceErrorType.Conflict, result.Error.Type);
        Assert.Equal("StorageLocation.CodeAlreadyExists", result.Error.Code);
        Assert.Equal("Storage location with the same code already exists in this warehouse.", result.Error.Message);
        Assert.Equal("code", result.Error.Property);

        int storageLocationCount = await testDbContext.DbContext.StorageLocations.CountAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(1, storageLocationCount);
    }

    [Fact]
    public async Task HandleAsync_WhenCommandIsValid_CreatesStorageLocationAndReturnsDetails()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        Warehouse warehouse = CreateWarehouse();
        Zone zone = CreateZone(warehouse.Id);
        StorageLocationType type = await GetStorageLocationTypeAsync(testDbContext.DbContext);
        StorageLocationStatus status = await GetStorageLocationStatusAsync(testDbContext.DbContext);

        testDbContext.DbContext.Warehouses.Add(warehouse);
        testDbContext.DbContext.Zones.Add(zone);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        CreateStorageLocation.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        CreateStorageLocation.Command command = new(
            WarehouseId: warehouse.Id,
            ZoneId: zone.Id,
            StorageLocationTypeId: type.Id,
            StorageLocationStatusId: status.Id,
            Code: " a-01-01 ",
            Name: " A-01-01 ",
            Description: " Pick face ",
            IsPickable: true);

        ServiceResult<StorageLocationDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);

        StorageLocationDetails details = result.Value;

        Assert.NotEqual(Guid.Empty, details.Id);
        Assert.Equal(warehouse.Id, details.WarehouseId);
        Assert.Equal(zone.Id, details.ZoneId);
        Assert.Equal(type.Id, details.StorageLocationTypeId);
        Assert.Equal(status.Id, details.StorageLocationStatusId);
        Assert.Equal("A-01-01", details.Code);
        Assert.Equal("A-01-01", details.Name);
        Assert.Equal("Pick face", details.Description);
        Assert.True(details.IsPickable);
        Assert.True(details.IsActive);

        StorageLocation storageLocation = await testDbContext.DbContext.StorageLocations.SingleAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(details.Id, storageLocation.Id);
        Assert.Equal(warehouse.Id, storageLocation.WarehouseId);
        Assert.Equal(zone.Id, storageLocation.ZoneId);
        Assert.Equal(type.Id, storageLocation.StorageLocationTypeId);
        Assert.Equal(status.Id, storageLocation.StorageLocationStatusId);
        Assert.Equal("A-01-01", storageLocation.Code);
        Assert.Equal("A-01-01", storageLocation.Name);
        Assert.Equal("Pick face", storageLocation.Description);
        Assert.True(storageLocation.IsPickable);

        Assert.NotEmpty(domainEventDispatcher.DispatchedEvents);
    }

    private static Warehouse CreateWarehouse(
        string code = "MAIN",
        string name = "Main Warehouse")
    {
        var result = Warehouse.Create(
            code,
            name,
            description: null,
            out Warehouse? warehouse);

        Assert.True(result.IsValid);
        Assert.NotNull(warehouse);

        return warehouse;
    }

    private static Zone CreateZone(Guid warehouseId)
    {
        var result = Zone.Create(
            warehouseId,
            code: "ZONE-A",
            name: "Zone A",
            description: null,
            out Zone? zone);

        Assert.True(result.IsValid);
        Assert.NotNull(zone);

        return zone;
    }

    private static async Task<StorageLocationType> GetStorageLocationTypeAsync(
        WmsDbContext dbContext)
    {
        return await dbContext.StorageLocationTypes.SingleAsync(
            x => x.Code == "PALLET_RACK",
            TestContext.Current.CancellationToken);
    }

    private static async Task<StorageLocationStatus> GetStorageLocationStatusAsync(
        WmsDbContext dbContext)
    {
        return await dbContext.StorageLocationStatuses.SingleAsync(
            x => x.Code == "AVAILABLE",
            TestContext.Current.CancellationToken);
    }
}