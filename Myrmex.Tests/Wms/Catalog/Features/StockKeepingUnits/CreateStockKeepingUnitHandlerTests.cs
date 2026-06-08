using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Catalog.Features.StockKeepingUnits;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Catalog.Features.StockKeepingUnits;

public sealed class CreateStockKeepingUnitHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenCommandIsInvalid_ReturnsInvalidServiceResult()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        CreateStockKeepingUnit.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        CreateStockKeepingUnit.Command command = new(
            Code: "",
            Name: "",
            Description: null);

        // Act
        ServiceResult<StockKeepingUnitDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);

        Assert.Equal(ServiceErrorType.Invalid, result.Error.Type);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Equal("One or more validation errors occurred.", result.Error.Message);

        Assert.Contains(result.Error.DetailList, error =>
            error.Code == "StockKeepingUnit.CodeRequired" &&
            error.Field == "code");

        Assert.Contains(result.Error.DetailList, error =>
            error.Code == "StockKeepingUnit.NameRequired" &&
            error.Field == "name");

        Assert.Empty(await testDbContext.DbContext.StockKeepingUnits.ToListAsync(
            TestContext.Current.CancellationToken));

        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenCodeAlreadyExists_ReturnsConflictServiceResult()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        CreateStockKeepingUnit.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        CreateStockKeepingUnit.Command firstCommand = new(
            Code: "ITEM-001",
            Name: "Widget",
            Description: null);

        ServiceResult<StockKeepingUnitDetails> firstResult = await handler.HandleAsync(
            firstCommand,
            TestContext.Current.CancellationToken);

        Assert.True(firstResult.IsSuccess);

        CreateStockKeepingUnit.Command duplicateCommand = new(
            Code: " item-001 ",
            Name: "Another Widget",
            Description: null);

        // Act
        ServiceResult<StockKeepingUnitDetails> result = await handler.HandleAsync(
            duplicateCommand,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);

        Assert.Equal(ServiceErrorType.Conflict, result.Error.Type);
        Assert.Equal("StockKeepingUnit.CodeAlreadyExists", result.Error.Code);
        Assert.Equal("Stock keeping unit with the same code already exists.", result.Error.Message);
        Assert.Equal("code", result.Error.Field);

        int stockKeepingUnitCount = await testDbContext.DbContext.StockKeepingUnits.CountAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(1, stockKeepingUnitCount);
    }

    [Fact]
    public async Task HandleAsync_WhenCommandIsValid_CreatesSkuAndReturnsDetails()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        CreateStockKeepingUnit.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        CreateStockKeepingUnit.Command command = new(
            Code: " item-001 ",
            Name: " Widget ",
            Description: " Sellable widget ");

        // Act
        ServiceResult<StockKeepingUnitDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);

        StockKeepingUnitDetails details = result.Value;

        Assert.NotEqual(Guid.Empty, details.Id);
        Assert.Equal("ITEM-001", details.Code);
        Assert.Equal("Widget", details.Name);
        Assert.Equal("Sellable widget", details.Description);
        Assert.True(details.IsActive);
        Assert.Null(details.UpdatedAtUtc);

        var stockKeepingUnit = await testDbContext.DbContext.StockKeepingUnits.SingleAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(details.Id, stockKeepingUnit.Id);
        Assert.Equal("ITEM-001", stockKeepingUnit.Code);
        Assert.Equal("Widget", stockKeepingUnit.Name);
        Assert.Equal("Sellable widget", stockKeepingUnit.Description);
        Assert.True(stockKeepingUnit.IsActive);
        Assert.Null(stockKeepingUnit.UpdatedAtUtc);

        var createdEvent = Assert.Single(domainEventDispatcher.DispatchedEvents);
        Assert.IsType<StockKeepingUnitCreatedDomainEvent>(createdEvent);
    }
}
