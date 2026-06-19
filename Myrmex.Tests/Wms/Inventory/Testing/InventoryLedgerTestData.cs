using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryTransactions;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;
using Myrmex.Modules.Wms.Topology.Domain.Zones;
using System.Reflection;

namespace Myrmex.Tests.Wms.Inventory.Testing;

internal static class InventoryLedgerTestData
{
    internal static async Task<SeededInventoryLedger> SeedLedgerAsync(
        WmsDbContext dbContext)
    {
        UnitOfMeasure each = CreateUnitOfMeasure("EA-LDG", "Ledger Each", "ea");
        UnitOfMeasure caseUnit = CreateUnitOfMeasure("CS-LDG", "Ledger Case", "cs");
        StockKeepingUnit skuA = CreateStockKeepingUnit("LEDGER-SKU-A", "Alpha Ledger Item", each.Id);
        StockKeepingUnit skuB = CreateStockKeepingUnit("LEDGER-SKU-B", "Beta Ledger Item", caseUnit.Id);
        Warehouse warehouseA = CreateWarehouse("LDG-WH-A", "Ledger Warehouse A");
        Warehouse warehouseB = CreateWarehouse("LDG-WH-B", "Ledger Warehouse B");
        Zone zoneA = CreateZone(warehouseA.Id, "LDG-ZONE-A", "Ledger Zone A");
        Zone zoneB = CreateZone(warehouseB.Id, "LDG-ZONE-B", "Ledger Zone B");
        StorageLocationType storageLocationType = dbContext.StorageLocationTypes.Single(x => x.Code == "PALLET_RACK");
        StorageLocationStatus storageLocationStatus = dbContext.StorageLocationStatuses.Single(x => x.Code == "AVAILABLE");
        StorageLocation locationA = CreateStorageLocation(
            warehouseA.Id,
            zoneA.Id,
            storageLocationType.Id,
            storageLocationStatus.Id,
            "LDG-LOC-A");
        StorageLocation locationB = CreateStorageLocation(
            warehouseB.Id,
            zoneB.Id,
            storageLocationType.Id,
            storageLocationStatus.Id,
            "LDG-LOC-B");
        StorageLocation inactiveLocation = CreateStorageLocation(
            warehouseA.Id,
            zoneA.Id,
            storageLocationType.Id,
            storageLocationStatus.Id,
            "LDG-LOC-INACTIVE");

        UnitOfMeasure inactiveUom = CreateUnitOfMeasure("INACTIVE-LDG", "Inactive Ledger UoM", "iu");
        StockKeepingUnit inactiveSku = CreateStockKeepingUnit("LEDGER-SKU-INACTIVE", "Inactive Ledger Item", inactiveUom.Id);
        Warehouse inactiveWarehouse = CreateWarehouse("LDG-WH-INACTIVE", "Inactive Ledger Warehouse");
        Zone inactiveZone = CreateZone(inactiveWarehouse.Id, "LDG-ZONE-INACTIVE", "Inactive Ledger Zone");
        StorageLocation inactiveWarehouseLocation = CreateStorageLocation(
            inactiveWarehouse.Id,
            inactiveZone.Id,
            storageLocationType.Id,
            storageLocationStatus.Id,
            "LDG-LOC-WH-INACTIVE");

        InventoryTransaction oldest = CreateAdjustment(
            skuA.Id,
            locationA.Id,
            balanceBefore: 5,
            balanceAfter: 8,
            reason: "Oldest adjustment",
            occurredAtUtc: DateTimeOffset.Parse("2026-06-18T08:00:00+00:00"));
        InventoryTransaction sameTimeFirst = CreateAdjustment(
            skuB.Id,
            locationB.Id,
            balanceBefore: 20,
            balanceAfter: 16,
            reason: "Same time first",
            occurredAtUtc: DateTimeOffset.Parse("2026-06-18T09:00:00+00:00"));
        InventoryTransaction sameTimeSecond = CreateAdjustment(
            skuA.Id,
            locationB.Id,
            balanceBefore: 8,
            balanceAfter: 10,
            reason: "Same time second",
            occurredAtUtc: DateTimeOffset.Parse("2026-06-18T09:00:00+00:00"));
        InventoryTransaction inactiveReferences = CreateAdjustment(
            inactiveSku.Id,
            inactiveWarehouseLocation.Id,
            balanceBefore: 12,
            balanceAfter: 9,
            reason: "Inactive references",
            occurredAtUtc: DateTimeOffset.Parse("2026-06-18T10:00:00+00:00"));

        inactiveUom.Deactivate();
        inactiveSku.Deactivate();
        inactiveWarehouse.Deactivate();
        inactiveWarehouseLocation.Deactivate();
        inactiveLocation.Deactivate();

        dbContext.UnitsOfMeasure.AddRange(each, caseUnit, inactiveUom);
        dbContext.StockKeepingUnits.AddRange(skuA, skuB, inactiveSku);
        dbContext.Warehouses.AddRange(warehouseA, warehouseB, inactiveWarehouse);
        dbContext.Zones.AddRange(zoneA, zoneB, inactiveZone);
        dbContext.StorageLocations.AddRange(locationA, locationB, inactiveLocation, inactiveWarehouseLocation);
        dbContext.InventoryTransactions.AddRange(oldest, sameTimeFirst, sameTimeSecond, inactiveReferences);

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return new SeededInventoryLedger(
            each,
            caseUnit,
            inactiveUom,
            skuA,
            skuB,
            inactiveSku,
            warehouseA,
            warehouseB,
            inactiveWarehouse,
            locationA,
            locationB,
            inactiveLocation,
            inactiveWarehouseLocation,
            oldest,
            sameTimeFirst,
            sameTimeSecond,
            inactiveReferences);
    }

    internal static InventoryBalance CreateInventoryBalance(
        Guid stockKeepingUnitId,
        Guid storageLocationId,
        decimal quantity)
    {
        var result = InventoryBalance.Create(
            stockKeepingUnitId,
            storageLocationId,
            quantity,
            out InventoryBalance? inventoryBalance);

        Assert.True(result.IsValid);
        Assert.NotNull(inventoryBalance);
        inventoryBalance.ClearDomainEvents();

        return inventoryBalance;
    }

    internal static InventoryTransaction CreateAdjustment(
        Guid stockKeepingUnitId,
        Guid storageLocationId,
        decimal balanceBefore,
        decimal balanceAfter,
        string reason,
        DateTimeOffset occurredAtUtc)
    {
        var result = InventoryTransaction.CreateAdjustment(
            stockKeepingUnitId,
            storageLocationId,
            balanceBefore,
            balanceAfter,
            reason,
            occurredAtUtc,
            out InventoryTransaction? transaction);

        Assert.True(result.IsValid);
        Assert.NotNull(transaction);
        transaction.ClearDomainEvents();

        return transaction;
    }

    internal static InventoryTransaction CreateMultiEntryTransaction(
        LedgerEntryInput firstEntry,
        LedgerEntryInput secondEntry,
        string reason,
        DateTimeOffset occurredAtUtc)
    {
        InventoryTransaction transaction = CreateAdjustment(
            firstEntry.StockKeepingUnitId,
            firstEntry.StorageLocationId,
            firstEntry.BalanceBefore,
            firstEntry.BalanceAfter,
            reason,
            occurredAtUtc);

        AddEntry(transaction, secondEntry);

        return transaction;
    }

    private static void AddEntry(
        InventoryTransaction transaction,
        LedgerEntryInput input)
    {
        var result = InventoryLedgerEntry.Create(
            input.StockKeepingUnitId,
            input.StorageLocationId,
            input.BalanceAfter - input.BalanceBefore,
            input.BalanceBefore,
            input.BalanceAfter,
            out InventoryLedgerEntry? entry);

        Assert.True(result.IsValid);
        Assert.NotNull(entry);

        FieldInfo entriesField = typeof(InventoryTransaction)
            .GetField("_entries", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("InventoryTransaction entries field was not found.");

        var entries = (List<InventoryLedgerEntry>)entriesField.GetValue(transaction)!;
        entries.Add(entry);
    }

    private static UnitOfMeasure CreateUnitOfMeasure(
        string code,
        string name,
        string? symbol)
    {
        var result = UnitOfMeasure.Create(
            code,
            name,
            symbol,
            out UnitOfMeasure? unitOfMeasure);

        Assert.True(result.IsValid);
        Assert.NotNull(unitOfMeasure);
        unitOfMeasure.ClearDomainEvents();

        return unitOfMeasure;
    }

    private static StockKeepingUnit CreateStockKeepingUnit(
        string code,
        string name,
        Guid baseUnitOfMeasureId)
    {
        var result = StockKeepingUnit.Create(
            code,
            name,
            description: null,
            baseUnitOfMeasureId,
            out StockKeepingUnit? stockKeepingUnit);

        Assert.True(result.IsValid);
        Assert.NotNull(stockKeepingUnit);
        stockKeepingUnit.ClearDomainEvents();

        return stockKeepingUnit;
    }

    private static Warehouse CreateWarehouse(
        string code,
        string name)
    {
        var result = Warehouse.Create(
            code,
            name,
            description: null,
            out Warehouse? warehouse);

        Assert.True(result.IsValid);
        Assert.NotNull(warehouse);
        warehouse.ClearDomainEvents();

        return warehouse;
    }

    private static Zone CreateZone(
        Guid warehouseId,
        string code,
        string name)
    {
        var result = Zone.Create(
            warehouseId,
            code,
            name,
            description: null,
            out Zone? zone);

        Assert.True(result.IsValid);
        Assert.NotNull(zone);
        zone.ClearDomainEvents();

        return zone;
    }

    private static StorageLocation CreateStorageLocation(
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

internal sealed record SeededInventoryLedger(
    UnitOfMeasure Each,
    UnitOfMeasure CaseUnit,
    UnitOfMeasure InactiveUom,
    StockKeepingUnit SkuA,
    StockKeepingUnit SkuB,
    StockKeepingUnit InactiveSku,
    Warehouse WarehouseA,
    Warehouse WarehouseB,
    Warehouse InactiveWarehouse,
    StorageLocation LocationA,
    StorageLocation LocationB,
    StorageLocation InactiveLocation,
    StorageLocation InactiveWarehouseLocation,
    InventoryTransaction Oldest,
    InventoryTransaction SameTimeFirst,
    InventoryTransaction SameTimeSecond,
    InventoryTransaction InactiveReferences)
{
    public IReadOnlyList<InventoryTransaction> Transactions =>
    [
        Oldest,
        SameTimeFirst,
        SameTimeSecond,
        InactiveReferences
    ];

    public IReadOnlyList<InventoryLedgerEntry> Entries =>
        Transactions.SelectMany(x => x.Entries).ToArray();
}

internal sealed record LedgerEntryInput(
    Guid StockKeepingUnitId,
    Guid StorageLocationId,
    decimal BalanceBefore,
    decimal BalanceAfter);
