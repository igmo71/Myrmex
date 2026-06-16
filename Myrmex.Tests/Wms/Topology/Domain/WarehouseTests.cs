using Myrmex.Modules.Wms.Topology.Domain.Warehouses;

namespace Myrmex.Tests.Wms.Topology.Domain;

public sealed class WarehouseTests
{
    [Fact]
    public void Create_WhenCodeIsMissing_ReturnsValidationError()
    {
        // Act
        var result = Warehouse.Create(
            code: "",
            name: "Main Warehouse",
            description: null,
            out Warehouse? warehouse);

        // Assert
        Assert.False(result.IsValid);
        Assert.Null(warehouse);

        var error = Assert.Single(result.Errors);

        Assert.Equal("Warehouse.CodeRequired", error.Code);
        Assert.Equal("Warehouse code is required.", error.Message);
        Assert.Equal("code", error.Property);
    }

    [Fact]
    public void Create_WhenNameIsMissing_ReturnsValidationError()
    {
        // Act
        var result = Warehouse.Create(
            code: "MAIN",
            name: "",
            description: null,
            out Warehouse? warehouse);

        // Assert
        Assert.False(result.IsValid);
        Assert.Null(warehouse);

        var error = Assert.Single(result.Errors);

        Assert.Equal("Warehouse.NameRequired", error.Code);
        Assert.Equal("Warehouse name is required.", error.Message);
        Assert.Equal("name", error.Property);
    }

    [Fact]
    public void Create_WhenValuesAreValid_NormalizesValuesAndCreatesWarehouse()
    {
        // Act
        var result = Warehouse.Create(
            code: " main ",
            name: " Main Warehouse ",
            description: " Primary warehouse ",
            out Warehouse? warehouse);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(warehouse);

        Assert.Equal("MAIN", warehouse.Code);
        Assert.Equal("Main Warehouse", warehouse.Name);
        Assert.Equal("Primary warehouse", warehouse.Description);
        Assert.True(warehouse.IsActive);
    }

    [Fact]
    public void UpdateDetails_WhenNameIsMissing_ReturnsValidationError()
    {
        // Arrange
        var createResult = Warehouse.Create(
            code: "MAIN",
            name: "Main Warehouse",
            description: null,
            out Warehouse? warehouse);

        Assert.True(createResult.IsValid);
        Assert.NotNull(warehouse);

        // Act
        var result = warehouse.UpdateDetails(
            name: "",
            description: null);

        // Assert
        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);

        Assert.Equal("Warehouse.NameRequired", error.Code);
        Assert.Equal("Warehouse name is required.", error.Message);
        Assert.Equal("name", error.Property);
    }

    [Fact]
    public void Deactivate_WhenWarehouseIsActive_MarksWarehouseInactive()
    {
        // Arrange
        var createResult = Warehouse.Create(
            code: "MAIN",
            name: "Main Warehouse",
            description: null,
            out Warehouse? warehouse);

        Assert.True(createResult.IsValid);
        Assert.NotNull(warehouse);
        Assert.True(warehouse.IsActive);

        // Act
        warehouse.Deactivate();

        // Assert
        Assert.False(warehouse.IsActive);
    }

    [Fact]
    public void Reactivate_WhenWarehouseIsInactive_MarksWarehouseActive()
    {
        // Arrange
        var createResult = Warehouse.Create(
            code: "MAIN",
            name: "Main Warehouse",
            description: null,
            out Warehouse? warehouse);

        Assert.True(createResult.IsValid);
        Assert.NotNull(warehouse);

        warehouse.Deactivate();
        Assert.False(warehouse.IsActive);

        // Act
        warehouse.Reactivate();

        // Assert
        Assert.True(warehouse.IsActive);
    }
}