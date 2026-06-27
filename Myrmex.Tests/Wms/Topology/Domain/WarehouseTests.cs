using Myrmex.Modules.Wms.Topology.Domain.Warehouses;

namespace Myrmex.Tests.Wms.Topology.Domain;

public sealed class WarehouseTests
{
    private static readonly Guid ExternalRefKey = Guid.Parse("018f0000-0000-7000-8000-000000000901");
    private static readonly DateTimeOffset ImportedAtUtc = DateTimeOffset.Parse("2026-06-27T09:00:00Z");

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

    [Fact]
    public void ApplyImport_UpdatesSourceOwnedDetailsMetadataAndLifecycle()
    {
        Warehouse warehouse = CreateWarehouse();

        var deleted = warehouse.ApplyImport(
            ExternalRefKey,
            code: null,
            name: null,
            isDeletionMarked: true,
            ImportedAtUtc);

        Assert.True(deleted.IsValid);
        Assert.Equal(ExternalRefKey, warehouse.ExternalRefKey);
        Assert.Equal(ImportedAtUtc, warehouse.LastImportedAtUtc);
        Assert.Equal("MAIN", warehouse.Code);
        Assert.Equal("Main Warehouse", warehouse.Name);
        Assert.Equal("Local description", warehouse.Description);
        Assert.False(warehouse.IsActive);

        var active = warehouse.ApplyImport(
            ExternalRefKey,
            "imported",
            "Imported Warehouse",
            isDeletionMarked: false,
            ImportedAtUtc.AddMinutes(1));

        Assert.True(active.IsValid);
        Assert.True(warehouse.IsActive);
        Assert.Equal("IMPORTED", warehouse.Code);
        Assert.Equal("Imported Warehouse", warehouse.Name);
        Assert.Equal(ImportedAtUtc.AddMinutes(1), warehouse.LastImportedAtUtc);
    }

    [Fact]
    public void ApplyImport_WhenExternalRefKeyChanges_RejectsMutation()
    {
        Warehouse warehouse = CreateWarehouse();
        Assert.True(warehouse.ApplyImport(ExternalRefKey, "MAIN", "Main", false, ImportedAtUtc).IsValid);

        var result = warehouse.ApplyImport(Guid.NewGuid(), "OTHER", "Other", false, ImportedAtUtc);

        Assert.False(result.IsValid);
        Assert.Equal(ExternalRefKey, warehouse.ExternalRefKey);
        Assert.Equal("MAIN", warehouse.Code);
    }

    private static Warehouse CreateWarehouse()
    {
        var result = Warehouse.Create("MAIN", "Main Warehouse", "Local description", out Warehouse? warehouse);
        Assert.True(result.IsValid);
        return Assert.IsType<Warehouse>(warehouse);
    }
}
