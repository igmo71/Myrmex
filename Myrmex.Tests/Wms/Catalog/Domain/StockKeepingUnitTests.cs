using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;

namespace Myrmex.Tests.Wms.Catalog.Domain;

public sealed class StockKeepingUnitTests
{
    [Fact]
    public void Create_WhenCodeIsMissing_ReturnsValidationError()
    {
        // Act
        var result = StockKeepingUnit.Create(
            code: "",
            name: "Widget",
            description: null,
            out StockKeepingUnit? stockKeepingUnit);

        // Assert
        Assert.False(result.IsValid);
        Assert.Null(stockKeepingUnit);

        var error = Assert.Single(result.Errors);

        Assert.Equal("StockKeepingUnit.CodeRequired", error.Code);
        Assert.Equal("SKU code is required.", error.Message);
        Assert.Equal("code", error.Field);
    }

    [Fact]
    public void Create_WhenNameIsMissing_ReturnsValidationError()
    {
        // Act
        var result = StockKeepingUnit.Create(
            code: "ITEM-001",
            name: "",
            description: null,
            out StockKeepingUnit? stockKeepingUnit);

        // Assert
        Assert.False(result.IsValid);
        Assert.Null(stockKeepingUnit);

        var error = Assert.Single(result.Errors);

        Assert.Equal("StockKeepingUnit.NameRequired", error.Code);
        Assert.Equal("SKU name is required.", error.Message);
        Assert.Equal("name", error.Field);
    }

    [Fact]
    public void Create_WhenDescriptionIsTooLong_ReturnsValidationError()
    {
        // Act
        var result = StockKeepingUnit.Create(
            code: "ITEM-001",
            name: "Widget",
            description: new string('A', StockKeepingUnit.MaxDescriptionLength + 1),
            out StockKeepingUnit? stockKeepingUnit);

        // Assert
        Assert.False(result.IsValid);
        Assert.Null(stockKeepingUnit);

        var error = Assert.Single(result.Errors);

        Assert.Equal("StockKeepingUnit.DescriptionTooLong", error.Code);
        Assert.Equal(
            $"SKU description must not exceed {StockKeepingUnit.MaxDescriptionLength} characters.",
            error.Message);
        Assert.Equal("description", error.Field);
    }

    [Fact]
    public void Create_WhenValuesAreValid_NormalizesValuesAndCreatesActiveSku()
    {
        // Act
        var result = StockKeepingUnit.Create(
            code: " item-001 ",
            name: " Widget ",
            description: " Sellable widget ",
            out StockKeepingUnit? stockKeepingUnit);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(stockKeepingUnit);

        Assert.NotEqual(Guid.Empty, stockKeepingUnit.Id);
        Assert.Equal("ITEM-001", stockKeepingUnit.Code);
        Assert.Equal("Widget", stockKeepingUnit.Name);
        Assert.Equal("Sellable widget", stockKeepingUnit.Description);
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
    public void UpdateDetails_WhenNameIsMissing_ReturnsValidationError()
    {
        // Arrange
        StockKeepingUnit stockKeepingUnit = CreateStockKeepingUnit();

        // Act
        var result = stockKeepingUnit.UpdateDetails(
            name: "",
            description: null);

        // Assert
        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);

        Assert.Equal("StockKeepingUnit.NameRequired", error.Code);
        Assert.Equal("SKU name is required.", error.Message);
        Assert.Equal("name", error.Field);
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
            description: " Updated description ");

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("ITEM-001", stockKeepingUnit.Code);
        Assert.Equal("Updated Widget", stockKeepingUnit.Name);
        Assert.Equal("Updated description", stockKeepingUnit.Description);
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

    private static StockKeepingUnit CreateStockKeepingUnit()
    {
        var result = StockKeepingUnit.Create(
            code: "ITEM-001",
            name: "Widget",
            description: null,
            out StockKeepingUnit? stockKeepingUnit);

        Assert.True(result.IsValid);
        Assert.NotNull(stockKeepingUnit);

        return stockKeepingUnit;
    }
}
