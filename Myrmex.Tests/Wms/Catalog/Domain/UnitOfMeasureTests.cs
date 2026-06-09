using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;

namespace Myrmex.Tests.Wms.Catalog.Domain;

public sealed class UnitOfMeasureTests
{
    [Fact]
    public void Create_WhenCodeIsMissing_ReturnsValidationError()
    {
        // Act
        var result = UnitOfMeasure.Create(
            code: "",
            name: "Each",
            symbol: null,
            out UnitOfMeasure? unitOfMeasure);

        // Assert
        Assert.False(result.IsValid);
        Assert.Null(unitOfMeasure);

        var error = Assert.Single(result.Errors);

        Assert.Equal("UnitOfMeasure.CodeRequired", error.Code);
        Assert.Equal("UoM code is required.", error.Message);
        Assert.Equal("code", error.Field);
    }

    [Fact]
    public void Create_WhenNameIsMissing_ReturnsValidationError()
    {
        // Act
        var result = UnitOfMeasure.Create(
            code: "EA",
            name: "",
            symbol: null,
            out UnitOfMeasure? unitOfMeasure);

        // Assert
        Assert.False(result.IsValid);
        Assert.Null(unitOfMeasure);

        var error = Assert.Single(result.Errors);

        Assert.Equal("UnitOfMeasure.NameRequired", error.Code);
        Assert.Equal("UoM name is required.", error.Message);
        Assert.Equal("name", error.Field);
    }

    [Fact]
    public void Create_WhenSymbolIsTooLong_ReturnsValidationError()
    {
        // Act
        var result = UnitOfMeasure.Create(
            code: "EA",
            name: "Each",
            symbol: new string('A', UnitOfMeasure.MaxSymbolLength + 1),
            out UnitOfMeasure? unitOfMeasure);

        // Assert
        Assert.False(result.IsValid);
        Assert.Null(unitOfMeasure);

        var error = Assert.Single(result.Errors);

        Assert.Equal("UnitOfMeasure.SymbolTooLong", error.Code);
        Assert.Equal(
            $"UoM symbol must not exceed {UnitOfMeasure.MaxSymbolLength} characters.",
            error.Message);
        Assert.Equal("symbol", error.Field);
    }

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
}
