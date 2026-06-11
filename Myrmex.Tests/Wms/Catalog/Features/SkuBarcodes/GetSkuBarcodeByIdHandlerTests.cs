using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.SkuBarcodes;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Catalog.Features.SkuBarcodes;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Catalog.Features.SkuBarcodes;

public sealed class GetSkuBarcodeByIdHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenSkuBarcodeIsActive_ReturnsDetails()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        StockKeepingUnit stockKeepingUnit = await AddStockKeepingUnitAsync(testDbContext);
        SkuBarcode skuBarcode = await AddSkuBarcodeAsync(
            testDbContext,
            stockKeepingUnit.Id,
            "ACTIVE-001",
            isActive: true);

        GetSkuBarcodeById.Handler handler = new(testDbContext.DbContext);

        // Act
        ServiceResult<SkuBarcodeDetails> result = await handler.HandleAsync(
            new GetSkuBarcodeById.Query(skuBarcode.Id),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(skuBarcode.Id, result.Value.Id);
        Assert.Equal(stockKeepingUnit.Id, result.Value.StockKeepingUnitId);
        Assert.Equal("ACTIVE-001", result.Value.Value);
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public async Task HandleAsync_WhenSkuBarcodeIsInactive_ReturnsDetails()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        StockKeepingUnit stockKeepingUnit = await AddStockKeepingUnitAsync(testDbContext);
        SkuBarcode skuBarcode = await AddSkuBarcodeAsync(
            testDbContext,
            stockKeepingUnit.Id,
            "INACTIVE-001",
            isActive: false);

        GetSkuBarcodeById.Handler handler = new(testDbContext.DbContext);

        // Act
        ServiceResult<SkuBarcodeDetails> result = await handler.HandleAsync(
            new GetSkuBarcodeById.Query(skuBarcode.Id),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(skuBarcode.Id, result.Value.Id);
        Assert.Equal("INACTIVE-001", result.Value.Value);
        Assert.False(result.Value.IsActive);
    }

    [Fact]
    public async Task HandleAsync_WhenSkuBarcodeDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        GetSkuBarcodeById.Handler handler = new(testDbContext.DbContext);

        // Act
        ServiceResult<SkuBarcodeDetails> result = await handler.HandleAsync(
            new GetSkuBarcodeById.Query(Guid.NewGuid()),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);

        Assert.Equal(ServiceErrorType.NotFound, result.Error.Type);
        Assert.Equal("SkuBarcode.NotFound", result.Error.Code);
        Assert.Equal("SKU barcode was not found.", result.Error.Message);
    }

    private static async Task<StockKeepingUnit> AddStockKeepingUnitAsync(TestWmsDbContext testDbContext)
    {
        UnitOfMeasure baseUnitOfMeasure = CreateUnitOfMeasure();

        testDbContext.DbContext.UnitsOfMeasure.Add(baseUnitOfMeasure);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = StockKeepingUnit.Create(
            code: "ITEM-001",
            name: "Widget",
            description: null,
            baseUnitOfMeasureId: baseUnitOfMeasure.Id,
            out StockKeepingUnit? stockKeepingUnit);

        Assert.True(result.IsValid);
        Assert.NotNull(stockKeepingUnit);

        testDbContext.DbContext.StockKeepingUnits.Add(stockKeepingUnit);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

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

    private static async Task<SkuBarcode> AddSkuBarcodeAsync(
        TestWmsDbContext testDbContext,
        Guid stockKeepingUnitId,
        string value,
        bool isActive)
    {
        var result = SkuBarcode.Create(
            stockKeepingUnitId,
            value,
            BarcodeSymbology.Code128,
            isPrimary: false,
            out SkuBarcode? skuBarcode);

        Assert.True(result.IsValid);
        Assert.NotNull(skuBarcode);

        skuBarcode.ClearDomainEvents();
        testDbContext.DbContext.SkuBarcodes.Add(skuBarcode);

        testDbContext.DbContext.Entry(skuBarcode)
            .Property(nameof(SkuBarcode.IsActive))
            .CurrentValue = isActive;

        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return skuBarcode;
    }
}
