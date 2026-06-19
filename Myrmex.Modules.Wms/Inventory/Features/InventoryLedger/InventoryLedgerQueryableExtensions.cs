using Myrmex.Modules.Wms.Inventory.Domain.InventoryTransactions;
using Myrmex.Shared.Wms.Inventory;

namespace Myrmex.Modules.Wms.Inventory.Features.InventoryLedger;

internal static class InventoryLedgerQueryableExtensions
{
    public static IQueryable<InventoryLedgerEntry> ApplyFilters(
        this IQueryable<InventoryLedgerEntry> queryable,
        ListInventoryLedgerEntries.Query query)
    {
        if (query.StockKeepingUnitId is Guid stockKeepingUnitId)
        {
            queryable = queryable.Where(x => x.StockKeepingUnitId == stockKeepingUnitId);
        }

        if (query.WarehouseId is Guid warehouseId)
        {
            queryable = queryable.Where(x => x.StorageLocation.WarehouseId == warehouseId);
        }

        if (query.StorageLocationId is Guid storageLocationId)
        {
            queryable = queryable.Where(x => x.StorageLocationId == storageLocationId);
        }

        if (!string.IsNullOrWhiteSpace(query.TransactionType))
        {
            queryable = queryable.Where(x => x.InventoryTransaction.TransactionType == InventoryTransactionType.Adjustment);
        }

        if (query.OccurredFromUtc is DateTimeOffset occurredFromUtc)
        {
            queryable = queryable.Where(x => x.InventoryTransaction.OccurredAtUtc >= occurredFromUtc);
        }

        if (query.OccurredToUtc is DateTimeOffset occurredToUtc)
        {
            queryable = queryable.Where(x => x.InventoryTransaction.OccurredAtUtc < occurredToUtc);
        }

        return queryable;
    }

    public static IOrderedQueryable<InventoryLedgerEntry> ApplySorting(
        this IQueryable<InventoryLedgerEntry> queryable,
        string? sortBy,
        bool sortDescending)
    {
        if (sortBy == InventoryLedgerSortBy.OccurredAtUtc)
            return Sort(
                queryable,
                x => x.InventoryTransaction.OccurredAtUtc,
                sortDescending);

        if (sortBy == InventoryLedgerSortBy.TransactionType)
            return Sort(
                queryable,
                x => x.InventoryTransaction.TransactionType,
                sortDescending);

        if (sortBy == InventoryLedgerSortBy.SkuCode)
            return Sort(
                queryable,
                x => x.StockKeepingUnit.Code,
                sortDescending);

        if (sortBy == InventoryLedgerSortBy.SkuName)
            return Sort(
                queryable,
                x => x.StockKeepingUnit.Name,
                sortDescending);

        if (sortBy == InventoryLedgerSortBy.WarehouseCode)
            return Sort(
                queryable,
                x => x.StorageLocation.Warehouse.Code,
                sortDescending);

        if (sortBy == InventoryLedgerSortBy.WarehouseName)
            return Sort(
                queryable,
                x => x.StorageLocation.Warehouse.Name,
                sortDescending);

        if (sortBy == InventoryLedgerSortBy.StorageLocationCode)
            return Sort(
                queryable,
                x => x.StorageLocation.Code,
                sortDescending);

        if (sortBy == InventoryLedgerSortBy.BalanceBefore)
            return Sort(
                queryable,
                x => x.BalanceBefore,
                sortDescending);

        if (sortBy == InventoryLedgerSortBy.QuantityDelta)
            return Sort(
                queryable,
                x => x.QuantityDelta,
                sortDescending);

        if (sortBy == InventoryLedgerSortBy.BalanceAfter)
            return Sort(
                queryable,
                x => x.BalanceAfter,
                sortDescending);

        if (sortBy == InventoryLedgerSortBy.Reason)
            return Sort(
                queryable,
                x => x.InventoryTransaction.Reason,
                sortDescending);

        return ApplyDefaultSorting(queryable);
    }

    public static IQueryable<InventoryLedgerEntryDetailsData> ProjectDetailsData(
        this IQueryable<InventoryLedgerEntry> queryable)
    {
        return queryable.Select(entry => new InventoryLedgerEntryDetailsData(
            entry.Id,
            entry.InventoryTransactionId,
            entry.InventoryTransaction.TransactionType,
            entry.InventoryTransaction.Reason,
            entry.InventoryTransaction.OccurredAtUtc,
            entry.BalanceBefore,
            entry.QuantityDelta,
            entry.BalanceAfter,
            new InventoryLedgerEntryDetailsData.StockKeepingUnitInfo(
                entry.StockKeepingUnitId,
                entry.StockKeepingUnit.Code,
                entry.StockKeepingUnit.Name,
                new InventoryLedgerEntryDetailsData.UnitOfMeasureInfo(
                    entry.StockKeepingUnit.BaseUnitOfMeasureId,
                    entry.StockKeepingUnit.BaseUnitOfMeasure.Code,
                    entry.StockKeepingUnit.BaseUnitOfMeasure.Symbol)),
            new InventoryLedgerEntryDetailsData.StorageLocationInfo(
                entry.StorageLocationId,
                entry.StorageLocation.Code,
                entry.StorageLocation.Name,
                new InventoryLedgerEntryDetailsData.WarehouseInfo(
                    entry.StorageLocation.WarehouseId,
                    entry.StorageLocation.Warehouse.Code,
                    entry.StorageLocation.Warehouse.Name))));
    }

    private static IOrderedQueryable<InventoryLedgerEntry> ApplyDefaultSorting(
        IQueryable<InventoryLedgerEntry> queryable)
    {
        return queryable
            .OrderByDescending(x => x.InventoryTransaction.OccurredAtUtc)
            .ThenByDescending(x => x.InventoryTransactionId)
            .ThenByDescending(x => x.Id);
    }

    private static IOrderedQueryable<InventoryLedgerEntry> Sort<TKey>(
        IQueryable<InventoryLedgerEntry> queryable,
        System.Linq.Expressions.Expression<Func<InventoryLedgerEntry, TKey>> keySelector,
        bool sortDescending)
    {
        return sortDescending
            ? queryable
                .OrderByDescending(keySelector)
                .ThenBy(x => x.InventoryTransactionId)
                .ThenBy(x => x.Id)
            : queryable
                .OrderBy(keySelector)
                .ThenBy(x => x.InventoryTransactionId)
                .ThenBy(x => x.Id);
    }
}

internal sealed record InventoryLedgerEntryDetailsData(
    Guid EntryId,
    Guid TransactionId,
    InventoryTransactionType TransactionType,
    string Reason,
    DateTimeOffset OccurredAtUtc,
    decimal BalanceBefore,
    decimal QuantityDelta,
    decimal BalanceAfter,
    InventoryLedgerEntryDetailsData.StockKeepingUnitInfo Sku,
    InventoryLedgerEntryDetailsData.StorageLocationInfo StorageLocation)
{
    public InventoryLedgerEntryDetails ToDetails()
    {
        return new InventoryLedgerEntryDetails(
            EntryId,
            TransactionId,
            TransactionType.ToString(),
            Reason,
            OccurredAtUtc,
            BalanceBefore,
            QuantityDelta,
            BalanceAfter,
            new InventoryLedgerEntryDetails.StockKeepingUnitInfo(
                Sku.Id,
                Sku.Code,
                Sku.Name,
                new InventoryLedgerEntryDetails.UnitOfMeasureInfo(
                    Sku.BaseUom.Id,
                    Sku.BaseUom.Code,
                    Sku.BaseUom.Symbol)),
            new InventoryLedgerEntryDetails.StorageLocationInfo(
                StorageLocation.Id,
                StorageLocation.Code,
                StorageLocation.Name,
                new InventoryLedgerEntryDetails.WarehouseInfo(
                    StorageLocation.Warehouse.Id,
                    StorageLocation.Warehouse.Code,
                    StorageLocation.Warehouse.Name)));
    }

    public sealed record StockKeepingUnitInfo(
        Guid Id,
        string Code,
        string Name,
        UnitOfMeasureInfo BaseUom);

    public sealed record UnitOfMeasureInfo(
        Guid Id,
        string Code,
        string? Symbol);

    public sealed record StorageLocationInfo(
        Guid Id,
        string Code,
        string Name,
        WarehouseInfo Warehouse);

    public sealed record WarehouseInfo(
        Guid Id,
        string Code,
        string Name);
}
