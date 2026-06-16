using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.SkuBarcodes;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Catalog.Features.SkuBarcodes;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Catalog.Features.SkuBarcodes;

public sealed class UpdateSkuBarcodeDetailsHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenValuesDifferOnlyByCase_UpdatesBarcode()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        StockKeepingUnit stockKeepingUnit = await AddStockKeepingUnitAsync(testDbContext);
        await AddSkuBarcodeAsync(testDbContext, stockKeepingUnit.Id, "abc");
        SkuBarcode skuBarcode = await AddSkuBarcodeAsync(testDbContext, stockKeepingUnit.Id, "DEF");

        UpdateSkuBarcodeDetails.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        UpdateSkuBarcodeDetails.Command command = new(
            SkuBarcodeId: skuBarcode.Id,
            Value: "ABC",
            Symbology: BarcodeSymbology.Code128,
            IsPrimary: false);

        ServiceResult<SkuBarcodeDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("ABC", result.Value.Value);

        string[] values = await testDbContext.DbContext.SkuBarcodes
            .OrderBy(x => x.Value)
            .Select(x => x.Value)
            .ToArrayAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["ABC", "abc"], values);
    }

    [Fact]
    public async Task HandleAsync_WhenCommandIsValid_UpdatesBarcodeAndReturnsDetails()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        StockKeepingUnit stockKeepingUnit = await AddStockKeepingUnitAsync(testDbContext);
        SkuBarcode skuBarcode = await AddSkuBarcodeAsync(testDbContext, stockKeepingUnit.Id, "ABC-123");

        UpdateSkuBarcodeDetails.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        UpdateSkuBarcodeDetails.Command command = new(
            SkuBarcodeId: skuBarcode.Id,
            Value: "  AbC-789  ",
            Symbology: BarcodeSymbology.QrCode,
            IsPrimary: true);

        ServiceResult<SkuBarcodeDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);

        SkuBarcodeDetails details = result.Value;

        Assert.Equal(skuBarcode.Id, details.Id);
        Assert.Equal(stockKeepingUnit.Id, details.StockKeepingUnitId);
        Assert.Equal("AbC-789", details.Value);
        Assert.Equal(BarcodeSymbology.QrCode, details.Symbology);
        Assert.True(details.IsPrimary);
        Assert.True(details.IsActive);
        Assert.NotNull(details.UpdatedAtUtc);

        var dispatchedEvent = Assert.Single(domainEventDispatcher.DispatchedEvents);
        Assert.IsType<SkuBarcodeDetailsUpdatedDomainEvent>(dispatchedEvent);
    }

    [Fact]
    public async Task HandleAsync_WhenIsPrimaryIsTrue_ClearsOtherActivePrimaryBarcodesForSku()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        StockKeepingUnit firstStockKeepingUnit = await AddStockKeepingUnitAsync(testDbContext, "ITEM-001");
        StockKeepingUnit secondStockKeepingUnit = await AddStockKeepingUnitAsync(testDbContext, "ITEM-002");

        SkuBarcode existingPrimary = await AddSkuBarcodeAsync(
            testDbContext,
            firstStockKeepingUnit.Id,
            "FIRST",
            isPrimary: true);
        SkuBarcode target = await AddSkuBarcodeAsync(
            testDbContext,
            firstStockKeepingUnit.Id,
            "SECOND",
            isPrimary: false);
        SkuBarcode otherSkuPrimary = await AddSkuBarcodeAsync(
            testDbContext,
            secondStockKeepingUnit.Id,
            "OTHER",
            isPrimary: true);

        UpdateSkuBarcodeDetails.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        UpdateSkuBarcodeDetails.Command command = new(
            SkuBarcodeId: target.Id,
            Value: "SECOND",
            Symbology: BarcodeSymbology.Code128,
            IsPrimary: true);

        ServiceResult<SkuBarcodeDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsPrimary);

        SkuBarcode refreshedExistingPrimary = await testDbContext.DbContext.SkuBarcodes
            .SingleAsync(x => x.Id == existingPrimary.Id, TestContext.Current.CancellationToken);
        SkuBarcode refreshedOtherSkuPrimary = await testDbContext.DbContext.SkuBarcodes
            .SingleAsync(x => x.Id == otherSkuPrimary.Id, TestContext.Current.CancellationToken);

        Assert.False(refreshedExistingPrimary.IsPrimary);
        Assert.True(refreshedOtherSkuPrimary.IsPrimary);
    }

    [Fact]
    public async Task HandleAsync_WhenIsPrimaryIsFalse_ClearsOnlyUpdatedBarcodePrimaryFlag()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        StockKeepingUnit stockKeepingUnit = await AddStockKeepingUnitAsync(testDbContext);
        SkuBarcode target = await AddSkuBarcodeAsync(
            testDbContext,
            stockKeepingUnit.Id,
            "PRIMARY",
            isPrimary: true);
        SkuBarcode otherBarcode = await AddSkuBarcodeAsync(
            testDbContext,
            stockKeepingUnit.Id,
            "OTHER",
            isPrimary: false);

        UpdateSkuBarcodeDetails.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        UpdateSkuBarcodeDetails.Command command = new(
            SkuBarcodeId: target.Id,
            Value: "PRIMARY",
            Symbology: BarcodeSymbology.Code128,
            IsPrimary: false);

        ServiceResult<SkuBarcodeDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsPrimary);

        SkuBarcode refreshedOtherBarcode = await testDbContext.DbContext.SkuBarcodes
            .SingleAsync(x => x.Id == otherBarcode.Id, TestContext.Current.CancellationToken);

        Assert.False(refreshedOtherBarcode.IsPrimary);
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
        bool isPrimary = false)
    {
        var result = SkuBarcode.Create(
            stockKeepingUnitId,
            value,
            symbology,
            isPrimary,
            out SkuBarcode? skuBarcode);

        Assert.True(result.IsValid);
        Assert.NotNull(skuBarcode);

        skuBarcode.ClearDomainEvents();
        testDbContext.DbContext.SkuBarcodes.Add(skuBarcode);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return skuBarcode;
    }
}
