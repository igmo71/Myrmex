using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Catalog.Features.StockKeepingUnits;
using Myrmex.Shared.Common;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Catalog.Features.StockKeepingUnits;

public sealed class ReactivateStockKeepingUnitHandlerTests
{

    [Fact]
    public async Task HandleAsync_WhenStockKeepingUnitIsInactive_ReactivatesAndReturnsToDefaultList()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        StockKeepingUnit stockKeepingUnit = await AddStockKeepingUnitAsync(testDbContext);
        stockKeepingUnit.Deactivate();
        stockKeepingUnit.ClearDomainEvents();
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
        Assert.Equal(stockKeepingUnit.BaseUnitOfMeasureId, details.BaseUnitOfMeasureId);
        Assert.True(details.IsActive);
        Assert.NotNull(details.UpdatedAtUtc);

        StockKeepingUnit persistedStockKeepingUnit = await testDbContext.DbContext.StockKeepingUnits.SingleAsync(
            TestContext.Current.CancellationToken);

        Assert.True(persistedStockKeepingUnit.IsActive);
        Assert.Equal(stockKeepingUnit.BaseUnitOfMeasureId, persistedStockKeepingUnit.BaseUnitOfMeasureId);

        ListStockKeepingUnits.Handler listHandler = new(testDbContext.DbContext);

        ServiceResult<ListResult<StockKeepingUnitDetails>> listResult = await listHandler.HandleAsync(
            new ListStockKeepingUnits.Query(),
            TestContext.Current.CancellationToken);

        Assert.True(listResult.IsSuccess);
        StockKeepingUnitDetails listedDetails = Assert.Single(listResult.Value.Items);
        Assert.Equal(stockKeepingUnit.Id, listedDetails.Id);
        Assert.Equal(stockKeepingUnit.BaseUnitOfMeasureId, listedDetails.BaseUnitOfMeasureId);

        var dispatchedEvent = Assert.Single(domainEventDispatcher.DispatchedEvents);
        Assert.IsType<StockKeepingUnitReactivatedDomainEvent>(dispatchedEvent);
    }

    [Fact]
    public async Task HandleAsync_WhenStockKeepingUnitIsAlreadyActive_ReturnsSuccessWithoutDispatchingNewDomainEvent()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        StockKeepingUnit stockKeepingUnit = await AddStockKeepingUnitAsync(testDbContext);

        ReactivateStockKeepingUnit.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        ReactivateStockKeepingUnit.Command command = new(stockKeepingUnit.Id);

        ServiceResult<StockKeepingUnitDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(stockKeepingUnit.BaseUnitOfMeasureId, result.Value.BaseUnitOfMeasureId);
        Assert.True(result.Value.IsActive);

        StockKeepingUnit persistedStockKeepingUnit = await testDbContext.DbContext.StockKeepingUnits.SingleAsync(
            TestContext.Current.CancellationToken);

        Assert.True(persistedStockKeepingUnit.IsActive);
        Assert.Equal(stockKeepingUnit.BaseUnitOfMeasureId, persistedStockKeepingUnit.BaseUnitOfMeasureId);
        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }

    private static async Task<StockKeepingUnit> AddStockKeepingUnitAsync(TestWmsDbContext testDbContext)
    {
        UnitOfMeasure baseUnitOfMeasure = CreateUnitOfMeasure();

        testDbContext.DbContext.UnitsOfMeasure.Add(baseUnitOfMeasure);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        StockKeepingUnit stockKeepingUnit = CreateStockKeepingUnit(baseUnitOfMeasure.Id);

        testDbContext.DbContext.StockKeepingUnits.Add(stockKeepingUnit);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return stockKeepingUnit;
    }

    private static StockKeepingUnit CreateStockKeepingUnit(Guid baseUnitOfMeasureId)
    {
        var result = StockKeepingUnit.Create(
            code: "ITEM-001",
            name: "Widget",
            description: null,
            baseUnitOfMeasureId: baseUnitOfMeasureId,
            out StockKeepingUnit? stockKeepingUnit);

        Assert.True(result.IsValid);
        Assert.NotNull(stockKeepingUnit);

        stockKeepingUnit.ClearDomainEvents();

        return stockKeepingUnit;
    }

    private static UnitOfMeasure CreateUnitOfMeasure()
    {
        var result = UnitOfMeasure.Create(
            code: "EA",
            name: "Each",
            symbol: "ea",
            out UnitOfMeasure? unitOfMeasure);

        Assert.True(result.IsValid);
        Assert.NotNull(unitOfMeasure);

        unitOfMeasure.ClearDomainEvents();

        return unitOfMeasure;
    }
}
