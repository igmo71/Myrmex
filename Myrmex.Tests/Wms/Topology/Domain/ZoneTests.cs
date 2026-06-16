using Myrmex.Modules.Wms.Topology.Domain.Zones;

namespace Myrmex.Tests.Wms.Topology.Domain;

public sealed class ZoneTests
{
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