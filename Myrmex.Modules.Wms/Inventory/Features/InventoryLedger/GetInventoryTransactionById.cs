using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryTransactions;
using Myrmex.Shared.Wms.Inventory;

namespace Myrmex.Modules.Wms.Inventory.Features.InventoryLedger;

internal static class GetInventoryTransactionById
{
    internal sealed record Query(Guid TransactionId) : IQuery<ServiceResult<InventoryTransactionDetails>>;

    internal sealed class Handler(WmsDbContext dbContext)
        : IQueryHandler<Query, ServiceResult<InventoryTransactionDetails>>
    {
        public async Task<ServiceResult<InventoryTransactionDetails>> HandleAsync(
            Query query,
            CancellationToken cancellationToken = default)
        {
            InventoryTransactionDetailsData? detailsData = await dbContext.InventoryTransactions
                .AsNoTracking()
                .Where(x => x.Id == query.TransactionId)
                .Select(transaction => new InventoryTransactionDetailsData(
                    transaction.Id,
                    transaction.TransactionType,
                    transaction.Reason,
                    transaction.OccurredAtUtc,
                    transaction.CreatedAtUtc,
                    transaction.Entries
                        .OrderBy(entry => entry.Id)
                        .Select(entry => new InventoryTransactionEntryDetailsData(
                            entry.Id,
                            entry.BalanceBefore,
                            entry.QuantityDelta,
                            entry.BalanceAfter,
                            new InventoryTransactionEntryDetailsData.StockKeepingUnitInfo(
                                entry.StockKeepingUnitId,
                                entry.StockKeepingUnit.Code,
                                entry.StockKeepingUnit.Name,
                                new InventoryTransactionEntryDetailsData.UnitOfMeasureInfo(
                                    entry.StockKeepingUnit.BaseUnitOfMeasureId,
                                    entry.StockKeepingUnit.BaseUnitOfMeasure.Code,
                                    entry.StockKeepingUnit.BaseUnitOfMeasure.Symbol)),
                            new InventoryTransactionEntryDetailsData.StorageLocationInfo(
                                entry.StorageLocationId,
                                entry.StorageLocation.Code,
                                entry.StorageLocation.Name,
                                new InventoryTransactionEntryDetailsData.WarehouseInfo(
                                    entry.StorageLocation.WarehouseId,
                                    entry.StorageLocation.Warehouse.Code,
                                    entry.StorageLocation.Warehouse.Name))))
                        .ToList()))
                .SingleOrDefaultAsync(cancellationToken);

            if (detailsData is null)
            {
                return ServiceResult<InventoryTransactionDetails>.Fail(
                    ServiceError.NotFound<InventoryTransaction>());
            }

            InventoryTransactionSourceDetails? source = await GetSourceAsync(
                dbContext,
                detailsData.Id,
                cancellationToken);

            return ServiceResult<InventoryTransactionDetails>.Success(detailsData.ToDetails(source));
        }

        private static async Task<InventoryTransactionSourceDetails?> GetSourceAsync(
            WmsDbContext dbContext,
            Guid transactionId,
            CancellationToken cancellationToken)
        {
            InventoryTransactionSourceDetails? receiving = await dbContext.ReceivingOrders
                .AsNoTracking()
                .Where(x => x.InventoryTransactionId == transactionId)
                .Select(x => new InventoryTransactionSourceDetails("ReceivingOrder", x.Number, x.CreatedAtUtc))
                .SingleOrDefaultAsync(cancellationToken);
            if (receiving is not null) return receiving;

            InventoryTransactionSourceDetails? transfer = await dbContext.InventoryTransferMovements
                .AsNoTracking()
                .Where(x => x.InventoryTransactionId == transactionId)
                .Select(x => new InventoryTransactionSourceDetails("InventoryTransfer", x.InventoryTransfer.Code, x.InventoryTransfer.CreatedAtUtc))
                .FirstOrDefaultAsync(cancellationToken);
            return transfer;
        }
    }
}

internal sealed record InventoryTransactionDetailsData(
    Guid Id,
    InventoryTransactionType TransactionType,
    string Reason,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<InventoryTransactionEntryDetailsData> Entries)
{
    public InventoryTransactionDetails ToDetails(InventoryTransactionSourceDetails? source)
    {
        return new InventoryTransactionDetails(
            Id,
            TransactionType.ToString(),
            Reason,
            OccurredAtUtc,
            CreatedAtUtc,
            Entries.Select(x => x.ToDetails()).ToArray(),
            source);
    }
}

internal sealed record InventoryTransactionEntryDetailsData(
    Guid EntryId,
    decimal BalanceBefore,
    decimal QuantityDelta,
    decimal BalanceAfter,
    InventoryTransactionEntryDetailsData.StockKeepingUnitInfo Sku,
    InventoryTransactionEntryDetailsData.StorageLocationInfo StorageLocation)
{
    public InventoryTransactionEntryDetails ToDetails()
    {
        return new InventoryTransactionEntryDetails(
            EntryId,
            BalanceBefore,
            QuantityDelta,
            BalanceAfter,
            new InventoryTransactionEntryDetails.StockKeepingUnitInfo(
                Sku.Id,
                Sku.Code,
                Sku.Name,
                new InventoryTransactionEntryDetails.UnitOfMeasureInfo(
                    Sku.BaseUom.Id,
                    Sku.BaseUom.Code,
                    Sku.BaseUom.Symbol)),
            new InventoryTransactionEntryDetails.StorageLocationInfo(
                StorageLocation.Id,
                StorageLocation.Code,
                StorageLocation.Name,
                new InventoryTransactionEntryDetails.WarehouseInfo(
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
