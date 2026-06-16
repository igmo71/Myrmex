using Myrmex.Modules.Wms.Topology.Domain.Zones;

namespace Myrmex.Tests.Wms.Topology.Domain;

public sealed class ZoneTests
{
    [Fact]
    public void Create_WhenWarehouseIdIsMissing_ReturnsValidationError()
    {
        var result = Zone.Create(
            warehouseId: Guid.Empty,
            code: "ZONE-A",
            name: "Zone A",
            description: null,
            out Zone? zone);

        Assert.False(result.IsValid);
        Assert.Null(zone);

        var error = Assert.Single(result.Errors);

        Assert.Equal("Zone.WarehouseIdRequired", error.Code);
        Assert.Equal("Warehouse id is required.", error.Message);
        Assert.Equal("warehouseId", error.Property);
    }

    [Fact]
    public void Create_WhenCodeIsMissing_ReturnsValidationError()
    {
        var result = Zone.Create(
            warehouseId: Guid.NewGuid(),
            code: "",
            name: "Zone A",
            description: null,
            out Zone? zone);

        Assert.False(result.IsValid);
        Assert.Null(zone);

        var error = Assert.Single(result.Errors);

        Assert.Equal("Zone.CodeRequired", error.Code);
        Assert.Equal("Zone code is required.", error.Message);
        Assert.Equal("code", error.Property);
    }

    [Fact]
    public void Create_WhenNameIsMissing_ReturnsValidationError()
    {
        var result = Zone.Create(
            warehouseId: Guid.NewGuid(),
            code: "ZONE-A",
            name: "",
            description: null,
            out Zone? zone);

        Assert.False(result.IsValid);
        Assert.Null(zone);

        var error = Assert.Single(result.Errors);

        Assert.Equal("Zone.NameRequired", error.Code);
        Assert.Equal("Zone name is required.", error.Message);
        Assert.Equal("name", error.Property);
    }

    [Fact]
    public void Create_WhenValuesAreValid_NormalizesValuesAndCreatesZone()
    {
        Guid warehouseId = Guid.NewGuid();

        var result = Zone.Create(
            warehouseId,
            code: " zone-a ",
            name: " Zone A ",
            description: " Picking zone ",
            out Zone? zone);

        Assert.True(result.IsValid);
        Assert.NotNull(zone);

        Assert.Equal(warehouseId, zone.WarehouseId);
        Assert.Equal("ZONE-A", zone.Code);
        Assert.Equal("Zone A", zone.Name);
        Assert.Equal("Picking zone", zone.Description);
        Assert.True(zone.IsActive);
    }

    [Fact]
    public void UpdateDetails_WhenNameIsMissing_ReturnsValidationError()
    {
        Zone zone = CreateValidZone();

        var result = zone.UpdateDetails(
            name: "",
            description: null);

        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);

        Assert.Equal("Zone.NameRequired", error.Code);
        Assert.Equal("Zone name is required.", error.Message);
        Assert.Equal("name", error.Property);
    }

    [Fact]
    public void Deactivate_WhenZoneIsActive_MarksZoneInactive()
    {
        Zone zone = CreateValidZone();

        zone.Deactivate();

        Assert.False(zone.IsActive);
    }

    [Fact]
    public void Reactivate_WhenZoneIsInactive_MarksZoneActive()
    {
        Zone zone = CreateValidZone();

        zone.Deactivate();
        Assert.False(zone.IsActive);

        zone.Reactivate();

        Assert.True(zone.IsActive);
    }

    private static Zone CreateValidZone()
    {
        var result = Zone.Create(
            warehouseId: Guid.NewGuid(),
            code: "ZONE-A",
            name: "Zone A",
            description: null,
            out Zone? zone);

        Assert.True(result.IsValid);
        Assert.NotNull(zone);

        return zone;
    }
}