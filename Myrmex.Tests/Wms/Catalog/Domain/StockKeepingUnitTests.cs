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
}
