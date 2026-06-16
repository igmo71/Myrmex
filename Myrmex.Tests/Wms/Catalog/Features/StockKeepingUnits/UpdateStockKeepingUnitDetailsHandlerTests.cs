using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Application.Queries;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Catalog.Features.StockKeepingUnits;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Catalog.Features.StockKeepingUnits;

public sealed class UpdateStockKeepingUnitDetailsHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenCommandIsValid_UpdatesStockKeepingUnitAndReturnsDetails()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        UnitOfMeasure originalBaseUnitOfMeasure = await AddUnitOfMeasureAsync(testDbContext, "EA");
        UnitOfMeasure newBaseUnitOfMeasure = await AddUnitOfMeasureAsync(testDbContext, "CS");
        StockKeepingUnit stockKeepingUnit = CreateStockKeepingUnit(originalBaseUnitOfMeasure.Id);

        testDbContext.DbContext.StockKeepingUnits.Add(stockKeepingUnit);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        UpdateStockKeepingUnitDetails.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        UpdateStockKeepingUnitDetails.Command command = new(
            StockKeepingUnitId: stockKeepingUnit.Id,
            Name: " Updated Widget ",
            Description: " Updated description ",
            BaseUnitOfMeasureId: newBaseUnitOfMeasure.Id);

        ServiceResult<StockKeepingUnitDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);

        StockKeepingUnitDetails details = result.Value;

        Assert.Equal(stockKeepingUnit.Id, details.Id);
        Assert.Equal("ITEM-001", details.Code);
        Assert.Equal("Updated Widget", details.Name);
        Assert.Equal("Updated description", details.Description);
        Assert.Equal(newBaseUnitOfMeasure.Id, details.BaseUnitOfMeasureId);
        Assert.True(details.IsActive);
        Assert.NotNull(details.UpdatedAtUtc);

        StockKeepingUnit persistedStockKeepingUnit = await testDbContext.DbContext.StockKeepingUnits.SingleAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal("ITEM-001", persistedStockKeepingUnit.Code);
        Assert.Equal("Updated Widget", persistedStockKeepingUnit.Name);
        Assert.Equal("Updated description", persistedStockKeepingUnit.Description);
        Assert.Equal(newBaseUnitOfMeasure.Id, persistedStockKeepingUnit.BaseUnitOfMeasureId);

        GetStockKeepingUnitById.Handler getHandler = new(testDbContext.DbContext);
        ServiceResult<StockKeepingUnitDetails> getResult = await getHandler.HandleAsync(
            new GetStockKeepingUnitById.Query(stockKeepingUnit.Id),
            TestContext.Current.CancellationToken);

        Assert.True(getResult.IsSuccess);
        Assert.Equal(newBaseUnitOfMeasure.Id, getResult.Value.BaseUnitOfMeasureId);

        ListStockKeepingUnits.Handler listHandler = new(testDbContext.DbContext);
        ServiceResult<ListResult<StockKeepingUnitDetails>> listResult = await listHandler.HandleAsync(
            new ListStockKeepingUnits.Query(),
            TestContext.Current.CancellationToken);

        Assert.True(listResult.IsSuccess);

        StockKeepingUnitDetails listedDetails = Assert.Single(listResult.Value.Items);
        Assert.Equal(stockKeepingUnit.Id, listedDetails.Id);
        Assert.Equal(newBaseUnitOfMeasure.Id, listedDetails.BaseUnitOfMeasureId);

        var dispatchedEvent = Assert.Single(domainEventDispatcher.DispatchedEvents);
        Assert.IsType<StockKeepingUnitDetailsUpdatedDomainEvent>(dispatchedEvent);
    }

    private static async Task<UnitOfMeasure> AddUnitOfMeasureAsync(
        TestWmsDbContext testDbContext,
        string code)
    {
        var result = UnitOfMeasure.Create(
            code: code,
            name: code,
            symbol: code.ToLowerInvariant(),
            out UnitOfMeasure? unitOfMeasure);

        Assert.True(result.IsValid);
        Assert.NotNull(unitOfMeasure);

        unitOfMeasure.ClearDomainEvents();

        testDbContext.DbContext.UnitsOfMeasure.Add(unitOfMeasure);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return unitOfMeasure;
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
}
