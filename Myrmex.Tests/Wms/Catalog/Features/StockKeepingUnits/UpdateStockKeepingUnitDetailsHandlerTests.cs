using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Catalog.Features.StockKeepingUnits;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Catalog.Features.StockKeepingUnits;

public sealed class UpdateStockKeepingUnitDetailsHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenStockKeepingUnitDoesNotExist_ReturnsNotFoundServiceResult()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        UpdateStockKeepingUnitDetails.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        UpdateStockKeepingUnitDetails.Command command = new(
            StockKeepingUnitId: Guid.NewGuid(),
            Name: "Updated Widget",
            Description: null);

        ServiceResult<StockKeepingUnitDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);

        Assert.Equal(ServiceErrorType.NotFound, result.Error.Type);
        Assert.Equal("StockKeepingUnit.NotFound", result.Error.Code);
        Assert.Equal("Stock keeping unit was not found.", result.Error.Message);
        Assert.Null(result.Error.Field);

        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenCommandIsInvalid_ReturnsInvalidServiceResult()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        StockKeepingUnit stockKeepingUnit = CreateStockKeepingUnit();

        testDbContext.DbContext.StockKeepingUnits.Add(stockKeepingUnit);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        UpdateStockKeepingUnitDetails.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        UpdateStockKeepingUnitDetails.Command command = new(
            StockKeepingUnitId: stockKeepingUnit.Id,
            Name: "",
            Description: null);

        ServiceResult<StockKeepingUnitDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);

        Assert.Equal(ServiceErrorType.Invalid, result.Error.Type);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Equal("One or more validation errors occurred.", result.Error.Message);

        var error = Assert.Single(result.Error.DetailList);

        Assert.Equal("StockKeepingUnit.NameRequired", error.Code);
        Assert.Equal("SKU name is required.", error.Message);
        Assert.Equal("name", error.Field);

        StockKeepingUnit persistedStockKeepingUnit = await testDbContext.DbContext.StockKeepingUnits.SingleAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal("Widget", persistedStockKeepingUnit.Name);
        Assert.Null(persistedStockKeepingUnit.Description);
        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenCommandIsValid_UpdatesStockKeepingUnitAndReturnsDetails()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        StockKeepingUnit stockKeepingUnit = CreateStockKeepingUnit();

        testDbContext.DbContext.StockKeepingUnits.Add(stockKeepingUnit);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        UpdateStockKeepingUnitDetails.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        UpdateStockKeepingUnitDetails.Command command = new(
            StockKeepingUnitId: stockKeepingUnit.Id,
            Name: " Updated Widget ",
            Description: " Updated description ");

        ServiceResult<StockKeepingUnitDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);

        StockKeepingUnitDetails details = result.Value;

        Assert.Equal(stockKeepingUnit.Id, details.Id);
        Assert.Equal("ITEM-001", details.Code);
        Assert.Equal("Updated Widget", details.Name);
        Assert.Equal("Updated description", details.Description);
        Assert.True(details.IsActive);
        Assert.NotNull(details.UpdatedAtUtc);

        StockKeepingUnit persistedStockKeepingUnit = await testDbContext.DbContext.StockKeepingUnits.SingleAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal("ITEM-001", persistedStockKeepingUnit.Code);
        Assert.Equal("Updated Widget", persistedStockKeepingUnit.Name);
        Assert.Equal("Updated description", persistedStockKeepingUnit.Description);

        var dispatchedEvent = Assert.Single(domainEventDispatcher.DispatchedEvents);
        Assert.IsType<StockKeepingUnitDetailsUpdatedDomainEvent>(dispatchedEvent);
    }

    private static StockKeepingUnit CreateStockKeepingUnit()
    {
        var result = StockKeepingUnit.Create(
            code: "ITEM-001",
            name: "Widget",
            description: null,
            out StockKeepingUnit? stockKeepingUnit);

        Assert.True(result.IsValid);
        Assert.NotNull(stockKeepingUnit);

        stockKeepingUnit.ClearDomainEvents();

        return stockKeepingUnit;
    }
}
