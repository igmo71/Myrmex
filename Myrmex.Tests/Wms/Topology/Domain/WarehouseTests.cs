using Myrmex.Modules.Wms.Topology.Domain.Warehouses;

namespace Myrmex.Tests.Wms.Topology.Domain;

public sealed class WarehouseTests
{
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