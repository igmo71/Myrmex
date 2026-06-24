using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Domain.Validation;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryCounts;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;
using Myrmex.Modules.Wms.Topology.Domain.Zones;

namespace Myrmex.Tests.Wms.Inventory.Testing;

internal static class InventoryCountTestData
{
    internal const string ActorId = "operator-001";

    internal static async Task<SeededInventoryCountReferences> SeedReferencesAsync(
        WmsDbContext dbContext,
        bool includeExistingBalance = true)
    {
        SeededInventoryTransferReferences references =
            await InventoryTransferTestData.SeedReferencesAsync(dbContext);

        Warehouse secondWarehouse = InventoryBalanceTestData.CreateWarehouse("SECOND", "Second Warehouse");
        Zone secondZone = InventoryBalanceTestData.CreateZone(secondWarehouse.Id, "ZONE-B", "Zone B");
        StorageLocation crossWarehouseLocation = InventoryTransferTestData.CreateStorageLocation(
            secondWarehouse.Id,
            secondZone.Id,
            references.RegularStorageLocationType.Id,
            references.StorageLocationStatus.Id,
            "B-01-01");

        dbContext.Warehouses.Add(secondWarehouse);
        dbContext.Zones.Add(secondZone);
        dbContext.StorageLocations.Add(crossWarehouseLocation);

        InventoryBalance? balance = null;

        if (includeExistingBalance)
        {
            DomainValidationResult balanceResult = InventoryBalance.Create(
                references.StockKeepingUnit.Id,
                references.SourceStorageLocation.Id,
                10,
                out balance);
            Assert.True(balanceResult.IsValid);
            Assert.NotNull(balance);
            balance.ClearDomainEvents();
            dbContext.InventoryBalances.Add(balance);
        }

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return new SeededInventoryCountReferences(
            references.BaseUnitOfMeasure,
            references.StockKeepingUnit,
            references.Warehouse,
            secondWarehouse,
            references.SourceStorageLocation,
            references.DestinationStorageLocation,
            references.InternalTransitStorageLocation,
            references.ExternalTransitStorageLocation,
            crossWarehouseLocation,
            balance);
    }

    internal static async Task<InventoryCount> CreateCountAsync(
        WmsDbContext dbContext,
        Guid warehouseId,
        string? reason = "Cycle count")
    {
        DomainValidationResult result = InventoryCount.Create(
            warehouseId,
            reason,
            ActorId,
            out InventoryCount? count);
        Assert.True(result.IsValid);
        Assert.NotNull(count);

        dbContext.InventoryCounts.Add(count);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return count;
    }
}

internal sealed record SeededInventoryCountReferences(
    UnitOfMeasure BaseUnitOfMeasure,
    StockKeepingUnit StockKeepingUnit,
    Warehouse Warehouse,
    Warehouse SecondWarehouse,
    StorageLocation ExistingBalanceLocation,
    StorageLocation MissingBalanceLocation,
    StorageLocation InternalTransitLocation,
    StorageLocation ExternalTransitLocation,
    StorageLocation CrossWarehouseLocation,
    InventoryBalance? ExistingBalance);
