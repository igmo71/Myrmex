using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;

namespace Myrmex.Tests.Wms.Catalog.Domain;

public sealed class UnitOfMeasureTests
{
    private static readonly Guid ExternalRefKey = Guid.Parse("018f0000-0000-7000-8000-000000000902");
    private static readonly DateTimeOffset ImportedAtUtc = DateTimeOffset.Parse("2026-06-27T09:00:00Z");

    [Fact]
    public void Create_WhenValuesAreValid_NormalizesValuesAndCreatesActiveUom()
    {
        // Act
        var result = UnitOfMeasure.Create(
            code: " ea ",
            name: " Each ",
            symbol: " ea ",
            out UnitOfMeasure? unitOfMeasure);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(unitOfMeasure);

        Assert.NotEqual(Guid.Empty, unitOfMeasure.Id);
        Assert.Equal("EA", unitOfMeasure.Code);
        Assert.Equal("Each", unitOfMeasure.Name);
        Assert.Equal("ea", unitOfMeasure.Symbol);
        Assert.True(unitOfMeasure.IsActive);
        Assert.Null(unitOfMeasure.UpdatedAtUtc);
    }

    [Fact]
    public void Create_WhenValuesAreValid_AddsCreatedDomainEvent()
    {
        // Act
        var result = UnitOfMeasure.Create(
            code: "EA",
            name: "Each",
            symbol: null,
            out UnitOfMeasure? unitOfMeasure);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(unitOfMeasure);

        var domainEvent = Assert.Single(unitOfMeasure.DomainEvents);

        UnitOfMeasureCreatedDomainEvent createdEvent =
            Assert.IsType<UnitOfMeasureCreatedDomainEvent>(domainEvent);

        Assert.Equal(unitOfMeasure.Id, createdEvent.UnitOfMeasureId);
    }

    [Fact]
    public void UpdateDetails_WhenValuesAreValid_UpdatesDetailsAndTimestamp()
    {
        // Arrange
        UnitOfMeasure unitOfMeasure = CreateUnitOfMeasure();
        unitOfMeasure.ClearDomainEvents();

        // Act
        var result = unitOfMeasure.UpdateDetails(
            name: " Updated Each ",
            symbol: " each ");

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("EA", unitOfMeasure.Code);
        Assert.Equal("Updated Each", unitOfMeasure.Name);
        Assert.Equal("each", unitOfMeasure.Symbol);
        Assert.NotNull(unitOfMeasure.UpdatedAtUtc);

        var domainEvent = Assert.Single(unitOfMeasure.DomainEvents);
        UnitOfMeasureDetailsUpdatedDomainEvent updatedEvent =
            Assert.IsType<UnitOfMeasureDetailsUpdatedDomainEvent>(domainEvent);

        Assert.Equal(unitOfMeasure.Id, updatedEvent.UnitOfMeasureId);
    }

    [Fact]
    public void Deactivate_WhenUnitOfMeasureIsActive_MarksInactiveAndAddsDomainEvent()
    {
        // Arrange
        UnitOfMeasure unitOfMeasure = CreateUnitOfMeasure();
        unitOfMeasure.ClearDomainEvents();

        // Act
        unitOfMeasure.Deactivate();

        // Assert
        Assert.False(unitOfMeasure.IsActive);
        Assert.NotNull(unitOfMeasure.UpdatedAtUtc);

        var domainEvent = Assert.Single(unitOfMeasure.DomainEvents);
        UnitOfMeasureDeactivatedDomainEvent deactivatedEvent =
            Assert.IsType<UnitOfMeasureDeactivatedDomainEvent>(domainEvent);

        Assert.Equal(unitOfMeasure.Id, deactivatedEvent.UnitOfMeasureId);
    }

    [Fact]
    public void Deactivate_WhenUnitOfMeasureIsAlreadyInactive_DoesNotAddDomainEventOrUpdateTimestamp()
    {
        // Arrange
        UnitOfMeasure unitOfMeasure = CreateUnitOfMeasure();
        unitOfMeasure.Deactivate();
        DateTimeOffset? updatedAtUtc = unitOfMeasure.UpdatedAtUtc;
        unitOfMeasure.ClearDomainEvents();

        // Act
        unitOfMeasure.Deactivate();

        // Assert
        Assert.False(unitOfMeasure.IsActive);
        Assert.Equal(updatedAtUtc, unitOfMeasure.UpdatedAtUtc);
        Assert.Empty(unitOfMeasure.DomainEvents);
    }

    [Fact]
    public void Reactivate_WhenUnitOfMeasureIsInactive_MarksActiveAndAddsDomainEvent()
    {
        // Arrange
        UnitOfMeasure unitOfMeasure = CreateUnitOfMeasure();
        unitOfMeasure.Deactivate();
        unitOfMeasure.ClearDomainEvents();

        // Act
        unitOfMeasure.Reactivate();

        // Assert
        Assert.True(unitOfMeasure.IsActive);
        Assert.NotNull(unitOfMeasure.UpdatedAtUtc);

        var domainEvent = Assert.Single(unitOfMeasure.DomainEvents);
        UnitOfMeasureReactivatedDomainEvent reactivatedEvent =
            Assert.IsType<UnitOfMeasureReactivatedDomainEvent>(domainEvent);

        Assert.Equal(unitOfMeasure.Id, reactivatedEvent.UnitOfMeasureId);
    }

    [Fact]
    public void Reactivate_WhenUnitOfMeasureIsAlreadyActive_DoesNotAddDomainEventOrUpdateTimestamp()
    {
        // Arrange
        UnitOfMeasure unitOfMeasure = CreateUnitOfMeasure();
        DateTimeOffset? updatedAtUtc = unitOfMeasure.UpdatedAtUtc;
        unitOfMeasure.ClearDomainEvents();

        // Act
        unitOfMeasure.Reactivate();

        // Assert
        Assert.True(unitOfMeasure.IsActive);
        Assert.Equal(updatedAtUtc, unitOfMeasure.UpdatedAtUtc);
        Assert.Empty(unitOfMeasure.DomainEvents);
    }

    [Fact]
    public void ApplyImport_UpdatesDetailsMetadataAndLifecycle()
    {
        UnitOfMeasure unitOfMeasure = CreateUnitOfMeasure();

        var deleted = unitOfMeasure.ApplyImport(
            ExternalRefKey,
            code: null,
            name: null,
            symbol: new string('x', UnitOfMeasure.MaxSymbolLength + 1),
            isDeletionMarked: true,
            ImportedAtUtc);

        Assert.True(deleted.IsValid);
        Assert.Equal(ExternalRefKey, unitOfMeasure.ExternalRefKey);
        Assert.Equal(ImportedAtUtc, unitOfMeasure.LastImportedAtUtc);
        Assert.Equal("EA", unitOfMeasure.Code);
        Assert.Equal("Each", unitOfMeasure.Name);
        Assert.Null(unitOfMeasure.Symbol);
        Assert.False(unitOfMeasure.IsActive);

        Assert.True(unitOfMeasure.ApplyImport(
            ExternalRefKey, "PKG", "Package", "pc", false, ImportedAtUtc.AddMinutes(1)).IsValid);
        Assert.True(unitOfMeasure.IsActive);
        Assert.Equal("PKG", unitOfMeasure.Code);
        Assert.Equal("Package", unitOfMeasure.Name);
        Assert.Equal("pc", unitOfMeasure.Symbol);
        Assert.Equal(ImportedAtUtc.AddMinutes(1), unitOfMeasure.LastImportedAtUtc);
    }

    [Fact]
    public void ApplyImport_WhenExternalRefKeyChanges_RejectsMutation()
    {
        UnitOfMeasure unitOfMeasure = CreateUnitOfMeasure();
        Assert.True(unitOfMeasure.ApplyImport(ExternalRefKey, "EA", "Each", "ea", false, ImportedAtUtc).IsValid);

        var result = unitOfMeasure.ApplyImport(Guid.NewGuid(), "KG", "Kilogram", "kg", false, ImportedAtUtc);

        Assert.False(result.IsValid);
        Assert.Equal(ExternalRefKey, unitOfMeasure.ExternalRefKey);
        Assert.Equal("EA", unitOfMeasure.Code);
    }

    private static UnitOfMeasure CreateUnitOfMeasure()
    {
        var result = UnitOfMeasure.Create(
            code: "EA",
            name: "Each",
            symbol: null,
            out UnitOfMeasure? unitOfMeasure);

        Assert.True(result.IsValid);
        Assert.NotNull(unitOfMeasure);

        return unitOfMeasure;
    }
}
