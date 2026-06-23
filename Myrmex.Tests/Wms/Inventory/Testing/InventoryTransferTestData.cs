using Microsoft.EntityFrameworkCore;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;
using Myrmex.Modules.Wms.Topology.Domain.Zones;

namespace Myrmex.Tests.Wms.Inventory.Testing;

internal static class InventoryTransferTestData
{
    internal static async Task<SeededInventoryTransferReferences> SeedReferencesAsync(
        WmsDbContext dbContext)
    {
        UnitOfMeasure baseUnitOfMeasure = InventoryBalanceTestData.CreateUnitOfMeasure();
        StockKeepingUnit stockKeepingUnit = InventoryBalanceTestData.CreateStockKeepingUnit(baseUnitOfMeasure.Id);
        Warehouse warehouse = InventoryBalanceTestData.CreateWarehouse();
        Zone zone = InventoryBalanceTestData.CreateZone(warehouse.Id);

        StorageLocationType regularType = await dbContext.StorageLocationTypes.SingleAsync(
            x => x.Code == "PALLET_RACK",
            TestContext.Current.CancellationToken);
        StorageLocationType internalTransitType = await dbContext.StorageLocationTypes.SingleAsync(
            x => x.Code == "INTERNAL_TRANSIT",
            TestContext.Current.CancellationToken);
        StorageLocationType externalTransitType = await dbContext.StorageLocationTypes.SingleAsync(
            x => x.Code == "EXTERNAL_TRANSIT",
            TestContext.Current.CancellationToken);
        StorageLocationStatus status = await dbContext.StorageLocationStatuses.SingleAsync(
            x => x.Code == "AVAILABLE",
            TestContext.Current.CancellationToken);

        StorageLocation sourceLocation = CreateStorageLocation(
            warehouse.Id,
            zone.Id,
            regularType.Id,
            status.Id,
            "A-01-01");
        StorageLocation destinationLocation = CreateStorageLocation(
            warehouse.Id,
            zone.Id,
            regularType.Id,
            status.Id,
            "A-01-02");
        StorageLocation internalTransitLocation = CreateStorageLocation(
            warehouse.Id,
            zone.Id,
            internalTransitType.Id,
            status.Id,
            "TR-IN-01");
        StorageLocation externalTransitLocation = CreateStorageLocation(
            warehouse.Id,
            zone.Id,
            externalTransitType.Id,
            status.Id,
            "TR-EX-01");

        dbContext.UnitsOfMeasure.Add(baseUnitOfMeasure);
        dbContext.StockKeepingUnits.Add(stockKeepingUnit);
        dbContext.Warehouses.Add(warehouse);
        dbContext.Zones.Add(zone);
        dbContext.StorageLocations.AddRange(
            sourceLocation,
            destinationLocation,
            internalTransitLocation,
            externalTransitLocation);

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return new SeededInventoryTransferReferences(
            baseUnitOfMeasure,
            stockKeepingUnit,
            warehouse,
            zone,
            regularType,
            internalTransitType,
            externalTransitType,
            status,
            sourceLocation,
            destinationLocation,
            internalTransitLocation,
            externalTransitLocation);
    }

    internal static StorageLocation CreateStorageLocation(
        Guid warehouseId,
        Guid zoneId,
        Guid storageLocationTypeId,
        Guid storageLocationStatusId,
        string code)
    {
        var result = StorageLocation.Create(
            warehouseId,
            zoneId,
            storageLocationTypeId,
            storageLocationStatusId,
            code,
            name: code,
            description: null,
            isPickable: true,
            out StorageLocation? storageLocation);

        Assert.True(result.IsValid);
        Assert.NotNull(storageLocation);
        storageLocation.ClearDomainEvents();

        return storageLocation;
    }
}

internal sealed record SeededInventoryTransferReferences(
    UnitOfMeasure BaseUnitOfMeasure,
    StockKeepingUnit StockKeepingUnit,
    Warehouse Warehouse,
    Zone Zone,
    StorageLocationType RegularStorageLocationType,
    StorageLocationType InternalTransitStorageLocationType,
    StorageLocationType ExternalTransitStorageLocationType,
    StorageLocationStatus StorageLocationStatus,
    StorageLocation SourceStorageLocation,
    StorageLocation DestinationStorageLocation,
    StorageLocation InternalTransitStorageLocation,
    StorageLocation ExternalTransitStorageLocation);
