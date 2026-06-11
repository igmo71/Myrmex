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
            Description: null,
            BaseUnitOfMeasureId: Guid.Parse("018f0000-0000-7000-8000-000000000111"));

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

        UnitOfMeasure baseUnitOfMeasure = await AddUnitOfMeasureAsync(testDbContext, "EA");
        StockKeepingUnit stockKeepingUnit = CreateStockKeepingUnit(baseUnitOfMeasure.Id);

        testDbContext.DbContext.StockKeepingUnits.Add(stockKeepingUnit);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        UpdateStockKeepingUnitDetails.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        UpdateStockKeepingUnitDetails.Command command = new(
            StockKeepingUnitId: stockKeepingUnit.Id,
            Name: "",
            Description: null,
            BaseUnitOfMeasureId: baseUnitOfMeasure.Id);

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
        Assert.Equal(baseUnitOfMeasure.Id, persistedStockKeepingUnit.BaseUnitOfMeasureId);
        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenBaseUnitOfMeasureIsMissing_ReturnsInvalidServiceResult()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        UnitOfMeasure baseUnitOfMeasure = await AddUnitOfMeasureAsync(testDbContext, "EA");
        StockKeepingUnit stockKeepingUnit = CreateStockKeepingUnit(baseUnitOfMeasure.Id);

        testDbContext.DbContext.StockKeepingUnits.Add(stockKeepingUnit);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        UpdateStockKeepingUnitDetails.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        UpdateStockKeepingUnitDetails.Command command = new(
            StockKeepingUnitId: stockKeepingUnit.Id,
            Name: "Updated Widget",
            Description: null,
            BaseUnitOfMeasureId: null);

        ServiceResult<StockKeepingUnitDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);

        Assert.Equal(ServiceErrorType.Invalid, result.Error.Type);
        Assert.Equal("StockKeepingUnit.BaseUnitOfMeasureRequired", result.Error.Code);
        Assert.Equal("SKU base unit of measure is required.", result.Error.Message);
        Assert.Equal("baseUnitOfMeasureId", result.Error.Field);

        StockKeepingUnit persistedStockKeepingUnit = await testDbContext.DbContext.StockKeepingUnits.SingleAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(baseUnitOfMeasure.Id, persistedStockKeepingUnit.BaseUnitOfMeasureId);
        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenBaseUnitOfMeasureDoesNotExist_ReturnsNotFoundServiceResult()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        UnitOfMeasure baseUnitOfMeasure = await AddUnitOfMeasureAsync(testDbContext, "EA");
        StockKeepingUnit stockKeepingUnit = CreateStockKeepingUnit(baseUnitOfMeasure.Id);

        testDbContext.DbContext.StockKeepingUnits.Add(stockKeepingUnit);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        UpdateStockKeepingUnitDetails.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        UpdateStockKeepingUnitDetails.Command command = new(
            StockKeepingUnitId: stockKeepingUnit.Id,
            Name: "Updated Widget",
            Description: null,
            BaseUnitOfMeasureId: Guid.NewGuid());

        ServiceResult<StockKeepingUnitDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);

        Assert.Equal(ServiceErrorType.NotFound, result.Error.Type);
        Assert.Equal("UnitOfMeasure.NotFound", result.Error.Code);
        Assert.Equal("Base unit of measure was not found.", result.Error.Message);
        Assert.Equal("baseUnitOfMeasureId", result.Error.Field);

        StockKeepingUnit persistedStockKeepingUnit = await testDbContext.DbContext.StockKeepingUnits.SingleAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(baseUnitOfMeasure.Id, persistedStockKeepingUnit.BaseUnitOfMeasureId);
        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenBaseUnitOfMeasureIsInactive_ReturnsFailureServiceResult()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        UnitOfMeasure originalBaseUnitOfMeasure = await AddUnitOfMeasureAsync(testDbContext, "EA");
        UnitOfMeasure inactiveBaseUnitOfMeasure = await AddUnitOfMeasureAsync(testDbContext, "CS");
        inactiveBaseUnitOfMeasure.Deactivate();
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        StockKeepingUnit stockKeepingUnit = CreateStockKeepingUnit(originalBaseUnitOfMeasure.Id);

        testDbContext.DbContext.StockKeepingUnits.Add(stockKeepingUnit);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        UpdateStockKeepingUnitDetails.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        UpdateStockKeepingUnitDetails.Command command = new(
            StockKeepingUnitId: stockKeepingUnit.Id,
            Name: "Updated Widget",
            Description: null,
            BaseUnitOfMeasureId: inactiveBaseUnitOfMeasure.Id);

        ServiceResult<StockKeepingUnitDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);

        Assert.Equal(ServiceErrorType.Invalid, result.Error.Type);
        Assert.Equal("StockKeepingUnit.BaseUnitOfMeasureInactive", result.Error.Code);
        Assert.Equal("SKU base unit of measure must be active.", result.Error.Message);
        Assert.Equal("baseUnitOfMeasureId", result.Error.Field);

        StockKeepingUnit persistedStockKeepingUnit = await testDbContext.DbContext.StockKeepingUnits.SingleAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(originalBaseUnitOfMeasure.Id, persistedStockKeepingUnit.BaseUnitOfMeasureId);
        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }

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
