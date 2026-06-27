using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;

namespace Myrmex.Tests.Wms.Catalog.Domain;

public sealed class StockKeepingUnitTests
{
    private static readonly Guid BaseUnitOfMeasureId = Guid.Parse("018f0000-0000-7000-8000-000000000111");
    private static readonly Guid OtherBaseUnitOfMeasureId = Guid.Parse("018f0000-0000-7000-8000-000000000112");
    private static readonly Guid ExternalRefKey = Guid.Parse("018f0000-0000-7000-8000-000000000903");
    private static readonly DateTimeOffset ImportedAtUtc = DateTimeOffset.Parse("2026-06-27T09:00:00Z");

    [Fact]
    public void Create_WhenValuesAreValid_NormalizesValuesAndCreatesActiveSku()
    {
        // Act
        var result = StockKeepingUnit.Create(
            code: " item-001 ",
            name: " Widget ",
            description: " Sellable widget ",
            baseUnitOfMeasureId: BaseUnitOfMeasureId,
            out StockKeepingUnit? stockKeepingUnit);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(stockKeepingUnit);

        Assert.NotEqual(Guid.Empty, stockKeepingUnit.Id);
        Assert.Equal("ITEM-001", stockKeepingUnit.Code);
        Assert.Equal("Widget", stockKeepingUnit.Name);
        Assert.Equal("Sellable widget", stockKeepingUnit.Description);
        Assert.Equal(BaseUnitOfMeasureId, stockKeepingUnit.BaseUnitOfMeasureId);
        Assert.True(stockKeepingUnit.IsActive);
        Assert.Null(stockKeepingUnit.UpdatedAtUtc);
    }

    [Fact]
    public void Create_WhenValuesAreValid_AddsCreatedDomainEvent()
    {
        // Act
        var result = StockKeepingUnit.Create(
            code: "ITEM-001",
            name: "Widget",
            description: null,
            baseUnitOfMeasureId: BaseUnitOfMeasureId,
            out StockKeepingUnit? stockKeepingUnit);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(stockKeepingUnit);

        var domainEvent = Assert.Single(stockKeepingUnit.DomainEvents);

        StockKeepingUnitCreatedDomainEvent createdEvent =
            Assert.IsType<StockKeepingUnitCreatedDomainEvent>(domainEvent);

        Assert.Equal(stockKeepingUnit.Id, createdEvent.StockKeepingUnitId);
    }

    [Fact]
    public void UpdateDetails_WhenValuesAreValid_UpdatesDetailsAndTimestamp()
    {
        // Arrange
        StockKeepingUnit stockKeepingUnit = CreateStockKeepingUnit();
        stockKeepingUnit.ClearDomainEvents();

        // Act
        var result = stockKeepingUnit.UpdateDetails(
            name: " Updated Widget ",
            description: " Updated description ",
            baseUnitOfMeasureId: BaseUnitOfMeasureId);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("ITEM-001", stockKeepingUnit.Code);
        Assert.Equal("Updated Widget", stockKeepingUnit.Name);
        Assert.Equal("Updated description", stockKeepingUnit.Description);
        Assert.Equal(BaseUnitOfMeasureId, stockKeepingUnit.BaseUnitOfMeasureId);
        Assert.NotNull(stockKeepingUnit.UpdatedAtUtc);

        var domainEvent = Assert.Single(stockKeepingUnit.DomainEvents);
        StockKeepingUnitDetailsUpdatedDomainEvent updatedEvent =
            Assert.IsType<StockKeepingUnitDetailsUpdatedDomainEvent>(domainEvent);

        Assert.Equal(stockKeepingUnit.Id, updatedEvent.StockKeepingUnitId);
    }

    [Fact]
    public void Deactivate_WhenStockKeepingUnitIsActive_MarksInactiveAndAddsDomainEvent()
    {
        // Arrange
        StockKeepingUnit stockKeepingUnit = CreateStockKeepingUnit();
        stockKeepingUnit.ClearDomainEvents();

        // Act
        stockKeepingUnit.Deactivate();

        // Assert
        Assert.False(stockKeepingUnit.IsActive);
        Assert.NotNull(stockKeepingUnit.UpdatedAtUtc);

        var domainEvent = Assert.Single(stockKeepingUnit.DomainEvents);
        StockKeepingUnitDeactivatedDomainEvent deactivatedEvent =
            Assert.IsType<StockKeepingUnitDeactivatedDomainEvent>(domainEvent);

        Assert.Equal(stockKeepingUnit.Id, deactivatedEvent.StockKeepingUnitId);
    }

    [Fact]
    public void Deactivate_WhenStockKeepingUnitIsAlreadyInactive_DoesNotAddDomainEventOrUpdateTimestamp()
    {
        // Arrange
        StockKeepingUnit stockKeepingUnit = CreateStockKeepingUnit();
        stockKeepingUnit.Deactivate();
        DateTimeOffset? updatedAtUtc = stockKeepingUnit.UpdatedAtUtc;
        stockKeepingUnit.ClearDomainEvents();

        // Act
        stockKeepingUnit.Deactivate();

        // Assert
        Assert.False(stockKeepingUnit.IsActive);
        Assert.Equal(updatedAtUtc, stockKeepingUnit.UpdatedAtUtc);
        Assert.Empty(stockKeepingUnit.DomainEvents);
    }

    [Fact]
    public void Reactivate_WhenStockKeepingUnitIsInactive_MarksActiveAndAddsDomainEvent()
    {
        // Arrange
        StockKeepingUnit stockKeepingUnit = CreateStockKeepingUnit();
        stockKeepingUnit.Deactivate();
        stockKeepingUnit.ClearDomainEvents();

        // Act
        stockKeepingUnit.Reactivate();

        // Assert
        Assert.True(stockKeepingUnit.IsActive);
        Assert.NotNull(stockKeepingUnit.UpdatedAtUtc);

        var domainEvent = Assert.Single(stockKeepingUnit.DomainEvents);
        StockKeepingUnitReactivatedDomainEvent reactivatedEvent =
            Assert.IsType<StockKeepingUnitReactivatedDomainEvent>(domainEvent);

        Assert.Equal(stockKeepingUnit.Id, reactivatedEvent.StockKeepingUnitId);
    }

    [Fact]
    public void Reactivate_WhenStockKeepingUnitIsAlreadyActive_DoesNotAddDomainEventOrUpdateTimestamp()
    {
        // Arrange
        StockKeepingUnit stockKeepingUnit = CreateStockKeepingUnit();
        DateTimeOffset? updatedAtUtc = stockKeepingUnit.UpdatedAtUtc;
        stockKeepingUnit.ClearDomainEvents();

        // Act
        stockKeepingUnit.Reactivate();

        // Assert
        Assert.True(stockKeepingUnit.IsActive);
        Assert.Equal(updatedAtUtc, stockKeepingUnit.UpdatedAtUtc);
        Assert.Empty(stockKeepingUnit.DomainEvents);
    }

    [Fact]
    public void ApplyImport_UpdatesDetailsBaseUnitMetadataAndLifecycle()
    {
        StockKeepingUnit stockKeepingUnit = CreateStockKeepingUnit();

        var deleted = stockKeepingUnit.ApplyImport(
            ExternalRefKey,
            " item-002 ",
            " Imported Widget ",
            OtherBaseUnitOfMeasureId,
            isDeletionMarked: true,
            ImportedAtUtc);

        Assert.True(deleted.IsValid);
        Assert.Equal(ExternalRefKey, stockKeepingUnit.ExternalRefKey);
        Assert.Equal(ImportedAtUtc, stockKeepingUnit.LastImportedAtUtc);
        Assert.Equal("ITEM-002", stockKeepingUnit.Code);
        Assert.Equal("Imported Widget", stockKeepingUnit.Name);
        Assert.Equal(OtherBaseUnitOfMeasureId, stockKeepingUnit.BaseUnitOfMeasureId);
        Assert.False(stockKeepingUnit.IsActive);

        Assert.True(stockKeepingUnit.ApplyImport(
            ExternalRefKey, "ITEM-002", "Imported Widget", OtherBaseUnitOfMeasureId,
            false, ImportedAtUtc.AddMinutes(1)).IsValid);
        Assert.True(stockKeepingUnit.IsActive);
    }

    [Fact]
    public void ApplyImport_WhenExternalRefKeyChanges_RejectsMutation()
    {
        StockKeepingUnit stockKeepingUnit = CreateStockKeepingUnit();
        Assert.True(stockKeepingUnit.ApplyImport(
            ExternalRefKey, "ITEM-001", "Widget", BaseUnitOfMeasureId, false, ImportedAtUtc).IsValid);

        var result = stockKeepingUnit.ApplyImport(
            Guid.NewGuid(), "ITEM-002", "Other", OtherBaseUnitOfMeasureId, false, ImportedAtUtc);

        Assert.False(result.IsValid);
        Assert.Equal(ExternalRefKey, stockKeepingUnit.ExternalRefKey);
        Assert.Equal("ITEM-001", stockKeepingUnit.Code);
        Assert.Equal(BaseUnitOfMeasureId, stockKeepingUnit.BaseUnitOfMeasureId);
    }

    private static StockKeepingUnit CreateStockKeepingUnit()
    {
        var result = StockKeepingUnit.Create(
            code: "ITEM-001",
            name: "Widget",
            description: null,
            baseUnitOfMeasureId: BaseUnitOfMeasureId,
            out StockKeepingUnit? stockKeepingUnit);

        Assert.True(result.IsValid);
        Assert.NotNull(stockKeepingUnit);

        return stockKeepingUnit;
    }
}
