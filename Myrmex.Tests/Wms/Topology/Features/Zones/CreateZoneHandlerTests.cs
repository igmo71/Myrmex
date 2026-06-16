using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;
using Myrmex.Modules.Wms.Topology.Features.Zones;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Topology.Features.Zones;

public sealed class CreateZoneHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenCommandIsValid_CreatesZoneAndReturnsDetails()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        Warehouse warehouse = CreateWarehouse();
        testDbContext.DbContext.Warehouses.Add(warehouse);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        CreateZone.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        CreateZone.Command command = new(
            WarehouseId: warehouse.Id,
            Code: " zone-a ",
            Name: " Zone A ",
            Description: " Picking zone ");

        ServiceResult<ZoneDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);

        ZoneDetails details = result.Value;

        Assert.NotEqual(Guid.Empty, details.Id);
        Assert.Equal(warehouse.Id, details.WarehouseId);
        Assert.Equal("ZONE-A", details.Code);
        Assert.Equal("Zone A", details.Name);
        Assert.Equal("Picking zone", details.Description);
        Assert.True(details.IsActive);

        var zone = await testDbContext.DbContext.Zones.SingleAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(details.Id, zone.Id);
        Assert.Equal(warehouse.Id, zone.WarehouseId);
        Assert.Equal("ZONE-A", zone.Code);
        Assert.Equal("Zone A", zone.Name);
        Assert.Equal("Picking zone", zone.Description);

        Assert.NotEmpty(domainEventDispatcher.DispatchedEvents);
    }

    private static Warehouse CreateWarehouse()
    {
        var result = Warehouse.Create(
            code: "MAIN",
            name: "Main Warehouse",
            description: null,
            out Warehouse? warehouse);

        Assert.True(result.IsValid);
        Assert.NotNull(warehouse);

        return warehouse;
    }
}