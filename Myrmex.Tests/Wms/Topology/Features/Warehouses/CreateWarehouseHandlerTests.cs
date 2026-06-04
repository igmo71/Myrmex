using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Topology.Features.Warehouses;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Topology.Features.Warehouses;

public sealed class CreateWarehouseHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenCommandIsInvalid_ReturnsInvalidServiceResult()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        CreateWarehouse.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        CreateWarehouse.Command command = new(
            Code: "",
            Name: "",
            Description: null);

        // Act
        ServiceResult<WarehouseDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);

        Assert.Equal(ServiceErrorType.Invalid, result.Error.Type);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Equal("One or more validation errors occurred.", result.Error.Message);

        Assert.Contains(result.Error.DetailList, error =>
            error.Code == "Warehouse.CodeRequired" &&
            error.Field == "code");

        Assert.Contains(result.Error.DetailList, error =>
            error.Code == "Warehouse.NameRequired" &&
            error.Field == "name");

        Assert.Empty(await testDbContext.DbContext.Warehouses.ToListAsync(
            TestContext.Current.CancellationToken));

        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenCodeAlreadyExists_ReturnsConflictServiceResult()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        CreateWarehouse.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        CreateWarehouse.Command firstCommand = new(
            Code: "MAIN",
            Name: "Main Warehouse",
            Description: null);

        ServiceResult<WarehouseDetails> firstResult = await handler.HandleAsync(
            firstCommand,
            TestContext.Current.CancellationToken);

        Assert.True(firstResult.IsSuccess);

        CreateWarehouse.Command duplicateCommand = new(
            Code: " main ",
            Name: "Another Warehouse",
            Description: null);

        // Act
        ServiceResult<WarehouseDetails> result = await handler.HandleAsync(
            duplicateCommand,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);

        Assert.Equal(ServiceErrorType.Conflict, result.Error.Type);
        Assert.Equal("Warehouse.CodeAlreadyExists", result.Error.Code);
        Assert.Equal("Warehouse with the same code already exists.", result.Error.Message);
        Assert.Equal("code", result.Error.Field);

        int warehouseCount = await testDbContext.DbContext.Warehouses.CountAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(1, warehouseCount);
    }

    [Fact]
    public async Task HandleAsync_WhenCommandIsValid_CreatesWarehouseAndReturnsDetails()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        CreateWarehouse.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        CreateWarehouse.Command command = new(
            Code: " main ",
            Name: " Main Warehouse ",
            Description: " Primary warehouse ");

        // Act
        ServiceResult<WarehouseDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);

        WarehouseDetails details = result.Value;

        Assert.NotEqual(Guid.Empty, details.Id);
        Assert.Equal("MAIN", details.Code);
        Assert.Equal("Main Warehouse", details.Name);
        Assert.Equal("Primary warehouse", details.Description);
        Assert.True(details.IsActive);

        var warehouse = await testDbContext.DbContext.Warehouses.SingleAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(details.Id, warehouse.Id);
        Assert.Equal("MAIN", warehouse.Code);
        Assert.Equal("Main Warehouse", warehouse.Name);
        Assert.Equal("Primary warehouse", warehouse.Description);
        Assert.True(warehouse.IsActive);

        Assert.NotEmpty(domainEventDispatcher.DispatchedEvents);
    }
}