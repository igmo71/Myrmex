using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Application.Queries;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Catalog.Features.StockKeepingUnits;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Catalog.Features.StockKeepingUnits;

public sealed class ReactivateStockKeepingUnitHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenStockKeepingUnitDoesNotExist_ReturnsNotFoundServiceResult()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        ReactivateStockKeepingUnit.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        ReactivateStockKeepingUnit.Command command = new(Guid.NewGuid());

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
    public async Task HandleAsync_WhenStockKeepingUnitIsInactive_ReactivatesAndReturnsToDefaultList()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        StockKeepingUnit stockKeepingUnit = CreateStockKeepingUnit();
        stockKeepingUnit.Deactivate();
        stockKeepingUnit.ClearDomainEvents();

        testDbContext.DbContext.StockKeepingUnits.Add(stockKeepingUnit);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        ReactivateStockKeepingUnit.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        ReactivateStockKeepingUnit.Command command = new(stockKeepingUnit.Id);

        ServiceResult<StockKeepingUnitDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);

        StockKeepingUnitDetails details = result.Value;

        Assert.Equal(stockKeepingUnit.Id, details.Id);
        Assert.True(details.IsActive);
        Assert.NotNull(details.UpdatedAtUtc);

        StockKeepingUnit persistedStockKeepingUnit = await testDbContext.DbContext.StockKeepingUnits.SingleAsync(
            TestContext.Current.CancellationToken);

        Assert.True(persistedStockKeepingUnit.IsActive);

        ListStockKeepingUnits.Handler listHandler = new(testDbContext.DbContext);

        ServiceResult<ListResult<StockKeepingUnitDetails>> listResult = await listHandler.HandleAsync(
            new ListStockKeepingUnits.Query(),
            TestContext.Current.CancellationToken);

        Assert.True(listResult.IsSuccess);
        StockKeepingUnitDetails listedDetails = Assert.Single(listResult.Value.Items);
        Assert.Equal(stockKeepingUnit.Id, listedDetails.Id);

        var dispatchedEvent = Assert.Single(domainEventDispatcher.DispatchedEvents);
        Assert.IsType<StockKeepingUnitReactivatedDomainEvent>(dispatchedEvent);
    }

    [Fact]
    public async Task HandleAsync_WhenStockKeepingUnitIsAlreadyActive_ReturnsSuccessWithoutDispatchingNewDomainEvent()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        StockKeepingUnit stockKeepingUnit = CreateStockKeepingUnit();

        testDbContext.DbContext.StockKeepingUnits.Add(stockKeepingUnit);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        ReactivateStockKeepingUnit.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        ReactivateStockKeepingUnit.Command command = new(stockKeepingUnit.Id);

        ServiceResult<StockKeepingUnitDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsActive);

        StockKeepingUnit persistedStockKeepingUnit = await testDbContext.DbContext.StockKeepingUnits.SingleAsync(
            TestContext.Current.CancellationToken);

        Assert.True(persistedStockKeepingUnit.IsActive);
        Assert.Empty(domainEventDispatcher.DispatchedEvents);
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
