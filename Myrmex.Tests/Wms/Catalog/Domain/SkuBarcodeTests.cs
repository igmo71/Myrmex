using Myrmex.Modules.Wms.Catalog.Domain.SkuBarcodes;

namespace Myrmex.Tests.Wms.Catalog.Domain;

public sealed class SkuBarcodeTests
{
    [Fact]
    public void Create_WhenValueIsMissing_ReturnsValidationError()
    {
        // Act
        var result = SkuBarcode.Create(
            stockKeepingUnitId: Guid.NewGuid(),
            value: "   ",
            symbology: BarcodeSymbology.Code128,
            isPrimary: false,
            out SkuBarcode? skuBarcode);

        // Assert
        Assert.False(result.IsValid);
        Assert.Null(skuBarcode);

        var error = Assert.Single(result.Errors);

        Assert.Equal("SkuBarcode.ValueRequired", error.Code);
        Assert.Equal("SKU barcode value is required.", error.Message);
        Assert.Equal("value", error.Field);
    }

    [Fact]
    public void Create_WhenValueIsTooLong_ReturnsValidationError()
    {
        // Act
        var result = SkuBarcode.Create(
            stockKeepingUnitId: Guid.NewGuid(),
            value: new string('A', SkuBarcode.MaxValueLength + 1),
            symbology: BarcodeSymbology.Code128,
            isPrimary: false,
            out SkuBarcode? skuBarcode);

        // Assert
        Assert.False(result.IsValid);
        Assert.Null(skuBarcode);

        var error = Assert.Single(result.Errors);

        Assert.Equal("SkuBarcode.ValueTooLong", error.Code);
        Assert.Equal(
            $"SKU barcode value must not exceed {SkuBarcode.MaxValueLength} characters.",
            error.Message);
        Assert.Equal("value", error.Field);
    }

    [Fact]
    public void Create_WhenSymbologyIsUnsupported_ReturnsValidationError()
    {
        // Act
        var result = SkuBarcode.Create(
            stockKeepingUnitId: Guid.NewGuid(),
            value: "ABC-123",
            symbology: (BarcodeSymbology)999,
            isPrimary: false,
            out SkuBarcode? skuBarcode);

        // Assert
        Assert.False(result.IsValid);
        Assert.Null(skuBarcode);

        var error = Assert.Single(result.Errors);

        Assert.Equal("SkuBarcode.SymbologyUnsupported", error.Code);
        Assert.Equal("SKU barcode symbology is not supported.", error.Message);
        Assert.Equal("symbology", error.Field);
    }

    [Fact]
    public void Create_WhenValuesAreValid_TrimsValueOnlyAndCreatesActiveBarcode()
    {
        // Arrange
        Guid stockKeepingUnitId = Guid.NewGuid();

        // Act
        var result = SkuBarcode.Create(
            stockKeepingUnitId,
            value: "  AbC-123  ",
            symbology: BarcodeSymbology.Code128,
            isPrimary: true,
            out SkuBarcode? skuBarcode);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(skuBarcode);

        Assert.NotEqual(Guid.Empty, skuBarcode.Id);
        Assert.Equal(stockKeepingUnitId, skuBarcode.StockKeepingUnitId);
        Assert.Equal("AbC-123", skuBarcode.Value);
        Assert.Equal(BarcodeSymbology.Code128, skuBarcode.Symbology);
        Assert.True(skuBarcode.IsPrimary);
        Assert.True(skuBarcode.IsActive);
        Assert.Null(skuBarcode.UpdatedAtUtc);
    }

    [Fact]
    public void Create_WhenValuesAreValid_AddsCreatedDomainEvent()
    {
        // Act
        var result = SkuBarcode.Create(
            stockKeepingUnitId: Guid.NewGuid(),
            value: "ABC-123",
            symbology: BarcodeSymbology.Internal,
            isPrimary: false,
            out SkuBarcode? skuBarcode);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(skuBarcode);

        var domainEvent = Assert.Single(skuBarcode.DomainEvents);

        SkuBarcodeCreatedDomainEvent createdEvent =
            Assert.IsType<SkuBarcodeCreatedDomainEvent>(domainEvent);

        Assert.Equal(skuBarcode.Id, createdEvent.SkuBarcodeId);
    }

    [Fact]
    public void ClearPrimary_WhenBarcodeIsPrimary_ClearsPrimaryAndTouchesTimestamp()
    {
        // Arrange
        SkuBarcode skuBarcode = CreateSkuBarcode(isPrimary: true);
        skuBarcode.ClearDomainEvents();

        // Act
        skuBarcode.ClearPrimary();

        // Assert
        Assert.False(skuBarcode.IsPrimary);
        Assert.NotNull(skuBarcode.UpdatedAtUtc);
        Assert.Empty(skuBarcode.DomainEvents);
    }

    [Fact]
    public void Model_DoesNotExposeNormalizedValue()
    {
        // Act
        var property = typeof(SkuBarcode).GetProperty("NormalizedValue");

        // Assert
        Assert.Null(property);
    }

    private static SkuBarcode CreateSkuBarcode(bool isPrimary)
    {
        var result = SkuBarcode.Create(
            stockKeepingUnitId: Guid.NewGuid(),
            value: "ABC-123",
            symbology: BarcodeSymbology.Code128,
            isPrimary,
            out SkuBarcode? skuBarcode);

        Assert.True(result.IsValid);
        Assert.NotNull(skuBarcode);

        return skuBarcode;
    }
}
