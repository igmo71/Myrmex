using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.SkuBarcodes;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Catalog.Features.SkuBarcodes;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Catalog.Features.SkuBarcodes;

public sealed class CreateSkuBarcodeHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenStockKeepingUnitDoesNotExist_ReturnsNotFoundServiceResult()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        CreateSkuBarcode.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        CreateSkuBarcode.Command command = new(
            StockKeepingUnitId: Guid.NewGuid(),
            Value: "ABC-123",
            Symbology: BarcodeSymbology.Code128,
            IsPrimary: false);

        // Act
        ServiceResult<SkuBarcodeDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);

        Assert.Equal(ServiceErrorType.NotFound, result.Error.Type);
        Assert.Equal("StockKeepingUnit.NotFound", result.Error.Code);
        Assert.Equal("stockKeepingUnitId", result.Error.Field);

        Assert.Empty(await testDbContext.DbContext.SkuBarcodes.ToListAsync(
            TestContext.Current.CancellationToken));
        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenValueIsBlankAfterTrim_ReturnsInvalidServiceResult()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        StockKeepingUnit stockKeepingUnit = await AddStockKeepingUnitAsync(testDbContext);

        CreateSkuBarcode.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        CreateSkuBarcode.Command command = new(
            StockKeepingUnitId: stockKeepingUnit.Id,
            Value: "   ",
            Symbology: BarcodeSymbology.Code128,
            IsPrimary: false);

        // Act
        ServiceResult<SkuBarcodeDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);

        Assert.Equal(ServiceErrorType.Invalid, result.Error.Type);
        Assert.Equal("Validation.Invalid", result.Error.Code);

        var detail = Assert.Single(result.Error.DetailList);
        Assert.Equal("SkuBarcode.ValueRequired", detail.Code);
        Assert.Equal("value", detail.Field);

        Assert.Empty(await testDbContext.DbContext.SkuBarcodes.ToListAsync(
            TestContext.Current.CancellationToken));
        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenTrimmedValueAlreadyExists_ReturnsConflictServiceResult()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        StockKeepingUnit stockKeepingUnit = await AddStockKeepingUnitAsync(testDbContext);

        CreateSkuBarcode.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        CreateSkuBarcode.Command firstCommand = new(
            StockKeepingUnitId: stockKeepingUnit.Id,
            Value: "AbC-123",
            Symbology: BarcodeSymbology.Code128,
            IsPrimary: false);

        ServiceResult<SkuBarcodeDetails> firstResult = await handler.HandleAsync(
            firstCommand,
            TestContext.Current.CancellationToken);

        Assert.True(firstResult.IsSuccess);

        CreateSkuBarcode.Command duplicateCommand = new(
            StockKeepingUnitId: stockKeepingUnit.Id,
            Value: "  AbC-123  ",
            Symbology: BarcodeSymbology.Code128,
            IsPrimary: false);

        // Act
        ServiceResult<SkuBarcodeDetails> result = await handler.HandleAsync(
            duplicateCommand,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);

        Assert.Equal(ServiceErrorType.Conflict, result.Error.Type);
        Assert.Equal("SkuBarcode.ValueAlreadyExists", result.Error.Code);
        Assert.Equal("value", result.Error.Field);

        int skuBarcodeCount = await testDbContext.DbContext.SkuBarcodes.CountAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(1, skuBarcodeCount);
    }

    [Fact]
    public async Task HandleAsync_WhenValuesDifferOnlyByCase_CreatesBothBarcodes()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        StockKeepingUnit stockKeepingUnit = await AddStockKeepingUnitAsync(testDbContext);

        CreateSkuBarcode.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        // Act
        ServiceResult<SkuBarcodeDetails> firstResult = await handler.HandleAsync(
            new CreateSkuBarcode.Command(
                StockKeepingUnitId: stockKeepingUnit.Id,
                Value: "abc",
                Symbology: BarcodeSymbology.Code128,
                IsPrimary: false),
            TestContext.Current.CancellationToken);

        ServiceResult<SkuBarcodeDetails> secondResult = await handler.HandleAsync(
            new CreateSkuBarcode.Command(
                StockKeepingUnitId: stockKeepingUnit.Id,
                Value: "ABC",
                Symbology: BarcodeSymbology.Code128,
                IsPrimary: false),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(firstResult.IsSuccess);
        Assert.True(secondResult.IsSuccess);

        string[] values = await testDbContext.DbContext.SkuBarcodes
            .OrderBy(x => x.Value)
            .Select(x => x.Value)
            .ToArrayAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["ABC", "abc"], values);
    }

    [Fact]
    public async Task HandleAsync_WhenIsPrimaryIsTrue_ClearsOtherActivePrimaryBarcodesForSku()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        StockKeepingUnit firstStockKeepingUnit = await AddStockKeepingUnitAsync(
            testDbContext,
            code: "ITEM-001");
        StockKeepingUnit secondStockKeepingUnit = await AddStockKeepingUnitAsync(
            testDbContext,
            code: "ITEM-002");

        SkuBarcode existingPrimary = CreateSkuBarcode(
            firstStockKeepingUnit.Id,
            "FIRST",
            isPrimary: true);
        SkuBarcode otherSkuPrimary = CreateSkuBarcode(
            secondStockKeepingUnit.Id,
            "OTHER",
            isPrimary: true);

        testDbContext.DbContext.SkuBarcodes.AddRange(existingPrimary, otherSkuPrimary);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        CreateSkuBarcode.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        CreateSkuBarcode.Command command = new(
            StockKeepingUnitId: firstStockKeepingUnit.Id,
            Value: "SECOND",
            Symbology: BarcodeSymbology.Code128,
            IsPrimary: true);

        // Act
        ServiceResult<SkuBarcodeDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsPrimary);

        SkuBarcode refreshedExistingPrimary = await testDbContext.DbContext.SkuBarcodes
            .SingleAsync(x => x.Id == existingPrimary.Id, TestContext.Current.CancellationToken);
        SkuBarcode refreshedOtherSkuPrimary = await testDbContext.DbContext.SkuBarcodes
            .SingleAsync(x => x.Id == otherSkuPrimary.Id, TestContext.Current.CancellationToken);

        Assert.False(refreshedExistingPrimary.IsPrimary);
        Assert.True(refreshedOtherSkuPrimary.IsPrimary);

        int activePrimaryCountForSku = await testDbContext.DbContext.SkuBarcodes
            .CountAsync(
                x => x.StockKeepingUnitId == firstStockKeepingUnit.Id &&
                     x.IsActive &&
                     x.IsPrimary,
                TestContext.Current.CancellationToken);

        Assert.Equal(1, activePrimaryCountForSku);
    }

    [Fact]
    public async Task HandleAsync_WhenCommandIsValid_CreatesBarcodeAndReturnsDetails()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        StockKeepingUnit stockKeepingUnit = await AddStockKeepingUnitAsync(testDbContext);

        CreateSkuBarcode.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        CreateSkuBarcode.Command command = new(
            StockKeepingUnitId: stockKeepingUnit.Id,
            Value: "  AbC-123  ",
            Symbology: BarcodeSymbology.Code128,
            IsPrimary: true);

        // Act
        ServiceResult<SkuBarcodeDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);

        SkuBarcodeDetails details = result.Value;

        Assert.NotEqual(Guid.Empty, details.Id);
        Assert.Equal(stockKeepingUnit.Id, details.StockKeepingUnitId);
        Assert.Equal("AbC-123", details.Value);
        Assert.Equal(BarcodeSymbology.Code128, details.Symbology);
        Assert.True(details.IsPrimary);
        Assert.True(details.IsActive);
        Assert.Null(details.UpdatedAtUtc);

        SkuBarcode skuBarcode = await testDbContext.DbContext.SkuBarcodes.SingleAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(details.Id, skuBarcode.Id);
        Assert.Equal("AbC-123", skuBarcode.Value);
        Assert.Equal(BarcodeSymbology.Code128, skuBarcode.Symbology);
        Assert.True(skuBarcode.IsPrimary);
        Assert.True(skuBarcode.IsActive);
        Assert.Null(skuBarcode.UpdatedAtUtc);

        var createdEvent = Assert.Single(domainEventDispatcher.DispatchedEvents);
        Assert.IsType<SkuBarcodeCreatedDomainEvent>(createdEvent);
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

    private static SkuBarcode CreateSkuBarcode(
        Guid stockKeepingUnitId,
        string value,
        bool isPrimary)
    {
        var result = SkuBarcode.Create(
            stockKeepingUnitId,
            value,
            BarcodeSymbology.Code128,
            isPrimary,
            out SkuBarcode? skuBarcode);

        Assert.True(result.IsValid);
        Assert.NotNull(skuBarcode);

        return skuBarcode;
    }
}
