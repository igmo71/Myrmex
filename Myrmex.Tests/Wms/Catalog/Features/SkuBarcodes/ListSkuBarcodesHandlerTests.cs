using Myrmex.Core.Application.Queries;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.SkuBarcodes;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Catalog.Features.SkuBarcodes;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Catalog.Features.SkuBarcodes;

public sealed class ListSkuBarcodesHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenIncludeInactiveIsFalse_ReturnsActiveSkuBarcodesOnly()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        StockKeepingUnit stockKeepingUnit = await AddStockKeepingUnitAsync(testDbContext);
        await AddSkuBarcodeAsync(testDbContext, stockKeepingUnit.Id, "ACTIVE-001", isActive: true);
        await AddSkuBarcodeAsync(testDbContext, stockKeepingUnit.Id, "INACTIVE-001", isActive: false);

        ListSkuBarcodes.Handler handler = new(testDbContext.DbContext);

        // Act
        ServiceResult<ListResult<SkuBarcodeDetails>> result = await handler.HandleAsync(
            new ListSkuBarcodes.Query(),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalCount);

        SkuBarcodeDetails details = Assert.Single(result.Value.Items);
        Assert.Equal("ACTIVE-001", details.Value);
        Assert.True(details.IsActive);
    }

    [Fact]
    public async Task HandleAsync_WhenIncludeInactiveIsTrue_ReturnsActiveAndInactiveSkuBarcodes()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        StockKeepingUnit stockKeepingUnit = await AddStockKeepingUnitAsync(testDbContext);
        await AddSkuBarcodeAsync(testDbContext, stockKeepingUnit.Id, "ACTIVE-001", isActive: true);
        await AddSkuBarcodeAsync(testDbContext, stockKeepingUnit.Id, "INACTIVE-001", isActive: false);

        ListSkuBarcodes.Handler handler = new(testDbContext.DbContext);

        var query = new ListSkuBarcodes.Query
        {
            IncludeInactive = true
        };

        // Act
        ServiceResult<ListResult<SkuBarcodeDetails>> result = await handler.HandleAsync(
            query,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Equal(["ACTIVE-001", "INACTIVE-001"], result.Value.Items.Select(x => x.Value).ToArray());
    }

    [Fact]
    public async Task HandleAsync_WhenStockKeepingUnitIdIsSupplied_ReturnsOnlyThatSkusBarcodes()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        StockKeepingUnit firstStockKeepingUnit = await AddStockKeepingUnitAsync(testDbContext, "ITEM-001");
        StockKeepingUnit secondStockKeepingUnit = await AddStockKeepingUnitAsync(testDbContext, "ITEM-002");
        await AddSkuBarcodeAsync(testDbContext, firstStockKeepingUnit.Id, "SKU1-001");
        await AddSkuBarcodeAsync(testDbContext, secondStockKeepingUnit.Id, "SKU2-001");

        ListSkuBarcodes.Handler handler = new(testDbContext.DbContext);

        var query = new ListSkuBarcodes.Query
        {
            StockKeepingUnitId = firstStockKeepingUnit.Id
        };

        // Act
        ServiceResult<ListResult<SkuBarcodeDetails>> result = await handler.HandleAsync(
            query,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        SkuBarcodeDetails details = Assert.Single(result.Value.Items);
        Assert.Equal(firstStockKeepingUnit.Id, details.StockKeepingUnitId);
        Assert.Equal("SKU1-001", details.Value);
    }

    [Fact]
    public async Task HandleAsync_WhenSearchTextMatchesValue_ReturnsMatchingSkuBarcodes()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        StockKeepingUnit stockKeepingUnit = await AddStockKeepingUnitAsync(testDbContext);
        await AddSkuBarcodeAsync(testDbContext, stockKeepingUnit.Id, "MATCH-001");
        await AddSkuBarcodeAsync(testDbContext, stockKeepingUnit.Id, "OTHER-001");

        ListSkuBarcodes.Handler handler = new(testDbContext.DbContext);

        var query = new ListSkuBarcodes.Query
        {
            SearchText = "MATCH"
        };

        // Act
        ServiceResult<ListResult<SkuBarcodeDetails>> result = await handler.HandleAsync(
            query,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        SkuBarcodeDetails details = Assert.Single(result.Value.Items);
        Assert.Equal("MATCH-001", details.Value);
    }

    [Fact]
    public async Task HandleAsync_WhenPagingValuesAreOutOfRange_NormalizesSkipAndTake()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        StockKeepingUnit stockKeepingUnit = await AddStockKeepingUnitAsync(testDbContext);
        await AddSkuBarcodeAsync(testDbContext, stockKeepingUnit.Id, "BARCODE-001");
        await AddSkuBarcodeAsync(testDbContext, stockKeepingUnit.Id, "BARCODE-002");
        await AddSkuBarcodeAsync(testDbContext, stockKeepingUnit.Id, "BARCODE-003");

        ListSkuBarcodes.Handler handler = new(testDbContext.DbContext);

        var query = new ListSkuBarcodes.Query
        {
            Skip = -5,
            Take = 1_000
        };

        // Act
        ServiceResult<ListResult<SkuBarcodeDetails>> result = await handler.HandleAsync(
            query,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.TotalCount);
        Assert.Equal(0, result.Value.Skip);
        Assert.Equal(ListQuery.MaxTake, result.Value.Take);
        Assert.Equal(3, result.Value.Items.Count);
    }

    [Theory]
    [InlineData("value")]
    [InlineData("symbology")]
    [InlineData("isActive")]
    public async Task HandleAsync_WhenSortByIsSupported_SortsByRequestedField(string sortBy)
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        StockKeepingUnit stockKeepingUnit = await AddStockKeepingUnitAsync(testDbContext);
        await AddSkuBarcodeAsync(
            testDbContext,
            stockKeepingUnit.Id,
            "BARCODE-A",
            BarcodeSymbology.Code128,
            isActive: false);
        await AddSkuBarcodeAsync(
            testDbContext,
            stockKeepingUnit.Id,
            "BARCODE-B",
            BarcodeSymbology.QrCode,
            isActive: true);

        ListSkuBarcodes.Handler handler = new(testDbContext.DbContext);

        var query = new ListSkuBarcodes.Query
        {
            SortBy = sortBy,
            IncludeInactive = true
        };

        // Act
        ServiceResult<ListResult<SkuBarcodeDetails>> result = await handler.HandleAsync(
            query,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(["BARCODE-A", "BARCODE-B"], result.Value.Items.Select(x => x.Value).ToArray());
    }

    [Fact]
    public async Task HandleAsync_WhenSortDescendingIsTrue_SortsDescending()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        StockKeepingUnit stockKeepingUnit = await AddStockKeepingUnitAsync(testDbContext);
        await AddSkuBarcodeAsync(testDbContext, stockKeepingUnit.Id, "BARCODE-A");
        await AddSkuBarcodeAsync(testDbContext, stockKeepingUnit.Id, "BARCODE-B");

        ListSkuBarcodes.Handler handler = new(testDbContext.DbContext);

        var query = new ListSkuBarcodes.Query
        {
            SortBy = "value",
            SortDescending = true
        };

        // Act
        ServiceResult<ListResult<SkuBarcodeDetails>> result = await handler.HandleAsync(
            query,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(["BARCODE-B", "BARCODE-A"], result.Value.Items.Select(x => x.Value).ToArray());
    }

    [Fact]
    public async Task HandleAsync_WhenSortByIsUnsupported_FallsBackToValueOrdering()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        StockKeepingUnit stockKeepingUnit = await AddStockKeepingUnitAsync(testDbContext);
        await AddSkuBarcodeAsync(testDbContext, stockKeepingUnit.Id, "BARCODE-B");
        await AddSkuBarcodeAsync(testDbContext, stockKeepingUnit.Id, "BARCODE-A");

        ListSkuBarcodes.Handler handler = new(testDbContext.DbContext);

        var query = new ListSkuBarcodes.Query
        {
            SortBy = "unknown",
            SortDescending = true
        };

        // Act
        ServiceResult<ListResult<SkuBarcodeDetails>> result = await handler.HandleAsync(
            query,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(["BARCODE-A", "BARCODE-B"], result.Value.Items.Select(x => x.Value).ToArray());
    }

    private static async Task<StockKeepingUnit> AddStockKeepingUnitAsync(
        TestWmsDbContext testDbContext,
        string code = "ITEM-001")
    {
        UnitOfMeasure baseUnitOfMeasure = CreateUnitOfMeasure(code);

        testDbContext.DbContext.UnitsOfMeasure.Add(baseUnitOfMeasure);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = StockKeepingUnit.Create(
            code,
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

    private static async Task<SkuBarcode> AddSkuBarcodeAsync(
        TestWmsDbContext testDbContext,
        Guid stockKeepingUnitId,
        string value,
        BarcodeSymbology symbology = BarcodeSymbology.Code128,
        bool isActive = true)
    {
        var result = SkuBarcode.Create(
            stockKeepingUnitId,
            value,
            symbology,
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
