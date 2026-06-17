using Myrmex.Modules.Wms.Catalog.Domain.SkuBarcodes;

namespace Myrmex.Tests.Wms.Catalog.Domain;

public sealed class SkuBarcodeTests
{
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
