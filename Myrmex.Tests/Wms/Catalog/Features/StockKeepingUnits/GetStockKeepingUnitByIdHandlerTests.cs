using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Catalog.Features.StockKeepingUnits;
using Myrmex.Shared.Wms.Catalog;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Catalog.Features.StockKeepingUnits;

public sealed class GetStockKeepingUnitByIdHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenStockKeepingUnitIsActive_ReturnsDetails()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        StockKeepingUnit stockKeepingUnit = await AddStockKeepingUnitAsync(
            testDbContext,
            "ITEM-001",
            "Widget",
            isActive: true);

        GetStockKeepingUnitById.Handler handler = new(testDbContext.DbContext);

        // Act
        ServiceResult<StockKeepingUnitDetails> result = await handler.HandleAsync(
            new GetStockKeepingUnitById.Query(stockKeepingUnit.Id),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(stockKeepingUnit.Id, result.Value.Id);
        Assert.Equal("ITEM-001", result.Value.Code);
        Assert.Equal(stockKeepingUnit.BaseUnitOfMeasureId, result.Value.BaseUnitOfMeasureId);
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public async Task HandleAsync_WhenStockKeepingUnitIsInactive_ReturnsDetails()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        StockKeepingUnit stockKeepingUnit = await AddStockKeepingUnitAsync(
            testDbContext,
            "ITEM-001",
            "Widget",
            isActive: false);

        GetStockKeepingUnitById.Handler handler = new(testDbContext.DbContext);

        // Act
        ServiceResult<StockKeepingUnitDetails> result = await handler.HandleAsync(
            new GetStockKeepingUnitById.Query(stockKeepingUnit.Id),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(stockKeepingUnit.Id, result.Value.Id);
        Assert.Equal("ITEM-001", result.Value.Code);
        Assert.Equal(stockKeepingUnit.BaseUnitOfMeasureId, result.Value.BaseUnitOfMeasureId);
        Assert.False(result.Value.IsActive);
    }

    private static async Task<StockKeepingUnit> AddStockKeepingUnitAsync(
        TestWmsDbContext testDbContext,
        string code,
        string name,
        bool isActive)
    {
        UnitOfMeasure baseUnitOfMeasure = CreateUnitOfMeasure(code);
        testDbContext.DbContext.UnitsOfMeasure.Add(baseUnitOfMeasure);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = StockKeepingUnit.Create(
            code,
            name,
            description: null,
            baseUnitOfMeasureId: baseUnitOfMeasure.Id,
            out StockKeepingUnit? stockKeepingUnit);

        Assert.True(result.IsValid);
        Assert.NotNull(stockKeepingUnit);

        stockKeepingUnit.ClearDomainEvents();
        testDbContext.DbContext.StockKeepingUnits.Add(stockKeepingUnit);

        testDbContext.DbContext.Entry(stockKeepingUnit)
            .Property(nameof(StockKeepingUnit.IsActive))
            .CurrentValue = isActive;

        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return stockKeepingUnit;
    }

    private static UnitOfMeasure CreateUnitOfMeasure(string skuCode)
    {
        string unitCode = skuCode.Replace("-", string.Empty);

        var result = UnitOfMeasure.Create(
            code: unitCode,
            name: unitCode,
            symbol: unitCode.ToLowerInvariant(),
            out UnitOfMeasure? unitOfMeasure);

        Assert.True(result.IsValid);
        Assert.NotNull(unitOfMeasure);

        unitOfMeasure.ClearDomainEvents();

        return unitOfMeasure;
    }
}
