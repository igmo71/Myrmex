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
        Assert.Equal("value", error.Property);
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
        Assert.Equal("value", error.Property);
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
        Assert.Equal("symbology", error.Property);
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
    public void UpdateDetails_WhenValuesAreValid_UpdatesDetailsAndTimestamp()
    {
        // Arrange
        SkuBarcode skuBarcode = CreateSkuBarcode(isPrimary: false);
        Guid stockKeepingUnitId = skuBarcode.StockKeepingUnitId;
        skuBarcode.ClearDomainEvents();

        // Act
        var result = skuBarcode.UpdateDetails(
            value: "  AbC-123  ",
            symbology: BarcodeSymbology.QrCode,
            isPrimary: true);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(stockKeepingUnitId, skuBarcode.StockKeepingUnitId);
        Assert.Equal("AbC-123", skuBarcode.Value);
        Assert.Equal(BarcodeSymbology.QrCode, skuBarcode.Symbology);
        Assert.True(skuBarcode.IsPrimary);
        Assert.True(skuBarcode.IsActive);
        Assert.NotNull(skuBarcode.UpdatedAtUtc);

        var domainEvent = Assert.Single(skuBarcode.DomainEvents);
        SkuBarcodeDetailsUpdatedDomainEvent updatedEvent =
            Assert.IsType<SkuBarcodeDetailsUpdatedDomainEvent>(domainEvent);

        Assert.Equal(skuBarcode.Id, updatedEvent.SkuBarcodeId);
    }

    [Fact]
    public void UpdateDetails_WhenValueIsMissing_ReturnsValidationError()
    {
        // Arrange
        SkuBarcode skuBarcode = CreateSkuBarcode(isPrimary: false);

        // Act
        var result = skuBarcode.UpdateDetails(
            value: "   ",
            symbology: BarcodeSymbology.Code128,
            isPrimary: false);

        // Assert
        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);

        Assert.Equal("SkuBarcode.ValueRequired", error.Code);
        Assert.Equal("value", error.Property);
        Assert.Equal("ABC-123", skuBarcode.Value);
    }

    [Fact]
    public void UpdateDetails_WhenSymbologyIsUnsupported_ReturnsValidationError()
    {
        // Arrange
        SkuBarcode skuBarcode = CreateSkuBarcode(isPrimary: false);

        // Act
        var result = skuBarcode.UpdateDetails(
            value: "ABC-123",
            symbology: (BarcodeSymbology)999,
            isPrimary: false);

        // Assert
        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);

        Assert.Equal("SkuBarcode.SymbologyUnsupported", error.Code);
        Assert.Equal("symbology", error.Property);
        Assert.Equal(BarcodeSymbology.Code128, skuBarcode.Symbology);
    }

    [Fact]
    public void UpdateDetails_WhenInactiveBarcodeIsMadePrimary_ReturnsUnsupportedPrimaryChange()
    {
        // Arrange
        SkuBarcode skuBarcode = CreateSkuBarcode(isPrimary: false);
        skuBarcode.Deactivate();
        skuBarcode.ClearDomainEvents();

        // Act
        var result = skuBarcode.UpdateDetails(
            value: "ABC-123",
            symbology: BarcodeSymbology.Code128,
            isPrimary: true);

        // Assert
        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);

        Assert.Equal("SkuBarcode.UnsupportedPrimaryChange", error.Code);
        Assert.Equal("isPrimary", error.Property);
        Assert.False(skuBarcode.IsActive);
        Assert.False(skuBarcode.IsPrimary);
        Assert.Empty(skuBarcode.DomainEvents);
    }

    [Fact]
    public void Deactivate_WhenBarcodeIsPrimary_ClearsPrimaryMarksInactiveAndAddsDomainEvent()
    {
        // Arrange
        SkuBarcode skuBarcode = CreateSkuBarcode(isPrimary: true);
        skuBarcode.ClearDomainEvents();

        // Act
        skuBarcode.Deactivate();

        // Assert
        Assert.False(skuBarcode.IsActive);
        Assert.False(skuBarcode.IsPrimary);
        Assert.NotNull(skuBarcode.UpdatedAtUtc);

        var domainEvent = Assert.Single(skuBarcode.DomainEvents);
        SkuBarcodeDeactivatedDomainEvent deactivatedEvent =
            Assert.IsType<SkuBarcodeDeactivatedDomainEvent>(domainEvent);

        Assert.Equal(skuBarcode.Id, deactivatedEvent.SkuBarcodeId);
    }

    [Fact]
    public void Deactivate_WhenBarcodeIsAlreadyInactive_DoesNotAddDomainEventOrUpdateTimestamp()
    {
        // Arrange
        SkuBarcode skuBarcode = CreateSkuBarcode(isPrimary: false);
        skuBarcode.Deactivate();
        DateTimeOffset? updatedAtUtc = skuBarcode.UpdatedAtUtc;
        skuBarcode.ClearDomainEvents();

        // Act
        skuBarcode.Deactivate();

        // Assert
        Assert.False(skuBarcode.IsActive);
        Assert.False(skuBarcode.IsPrimary);
        Assert.Equal(updatedAtUtc, skuBarcode.UpdatedAtUtc);
        Assert.Empty(skuBarcode.DomainEvents);
    }

    [Fact]
    public void Reactivate_WhenBarcodeIsInactive_MarksActiveNonPrimaryAndAddsDomainEvent()
    {
        // Arrange
        SkuBarcode skuBarcode = CreateSkuBarcode(isPrimary: true);
        skuBarcode.Deactivate();
        skuBarcode.ClearDomainEvents();

        // Act
        skuBarcode.Reactivate();

        // Assert
        Assert.True(skuBarcode.IsActive);
        Assert.False(skuBarcode.IsPrimary);
        Assert.NotNull(skuBarcode.UpdatedAtUtc);

        var domainEvent = Assert.Single(skuBarcode.DomainEvents);
        SkuBarcodeReactivatedDomainEvent reactivatedEvent =
            Assert.IsType<SkuBarcodeReactivatedDomainEvent>(domainEvent);

        Assert.Equal(skuBarcode.Id, reactivatedEvent.SkuBarcodeId);
    }

    [Fact]
    public void Reactivate_WhenBarcodeIsAlreadyActive_DoesNotAddDomainEventOrUpdateTimestamp()
    {
        // Arrange
        SkuBarcode skuBarcode = CreateSkuBarcode(isPrimary: true);
        DateTimeOffset? updatedAtUtc = skuBarcode.UpdatedAtUtc;
        skuBarcode.ClearDomainEvents();

        // Act
        skuBarcode.Reactivate();

        // Assert
        Assert.True(skuBarcode.IsActive);
        Assert.True(skuBarcode.IsPrimary);
        Assert.Equal(updatedAtUtc, skuBarcode.UpdatedAtUtc);
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
