using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Catalog.Features.StockKeepingUnits;
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
        Assert.False(result.Value.IsActive);
    }

    [Fact]
    public async Task HandleAsync_WhenStockKeepingUnitDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        GetStockKeepingUnitById.Handler handler = new(testDbContext.DbContext);

        // Act
        ServiceResult<StockKeepingUnitDetails> result = await handler.HandleAsync(
            new GetStockKeepingUnitById.Query(Guid.NewGuid()),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);

        Assert.Equal(ServiceErrorType.NotFound, result.Error.Type);
        Assert.Equal("StockKeepingUnit.NotFound", result.Error.Code);
        Assert.Equal("Stock keeping unit was not found.", result.Error.Message);
    }

    private static async Task<StockKeepingUnit> AddStockKeepingUnitAsync(
        TestWmsDbContext testDbContext,
        string code,
        string name,
        bool isActive)
    {
        var result = StockKeepingUnit.Create(
            code,
            name,
            description: null,
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
}
