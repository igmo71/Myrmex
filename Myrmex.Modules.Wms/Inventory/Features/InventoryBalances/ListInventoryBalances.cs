using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Application;
using Myrmex.Core.Application.Queries;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;

namespace Myrmex.Modules.Wms.Inventory.Features.InventoryBalances;

internal static class ListInventoryBalances
{
    internal sealed record Query : ListQuery, IQuery<ServiceResult<ListResult<InventoryBalanceDetails>>>
    {
        public Guid? StockKeepingUnitId { get; init; }

        public Guid? StorageLocationId { get; init; }

        public Guid? WarehouseId { get; init; }
    }

    internal sealed class Handler(WmsDbContext dbContext)
        : IQueryHandler<Query, ServiceResult<ListResult<InventoryBalanceDetails>>>
    {
        public async Task<ServiceResult<ListResult<InventoryBalanceDetails>>> HandleAsync(
            Query query,
            CancellationToken cancellationToken = default)
        {
            int skip = ListQuery.NormalizeSkip(query.Skip);
            int take = ListQuery.NormalizeTake(query.Take);

            IQueryable<InventoryBalance> inventoryBalances = dbContext.InventoryBalances
                .AsNoTracking();

            if (query.StockKeepingUnitId.HasValue)
            {
                inventoryBalances = inventoryBalances
                    .Where(x => x.StockKeepingUnitId == query.StockKeepingUnitId.Value);
            }

            if (query.StorageLocationId.HasValue)
            {
                inventoryBalances = inventoryBalances
                    .Where(x => x.StorageLocationId == query.StorageLocationId.Value);
            }

            if (query.WarehouseId.HasValue)
            {
                IQueryable<Guid> warehouseStorageLocationIds = dbContext.StorageLocations
                    .AsNoTracking()
                    .Where(x => x.WarehouseId == query.WarehouseId.Value)
                    .Select(x => x.Id);

                inventoryBalances = inventoryBalances
                    .Where(x => warehouseStorageLocationIds.Contains(x.StorageLocationId));
            }

            int totalCount = await inventoryBalances.CountAsync(cancellationToken);

            inventoryBalances = ApplySorting(
                inventoryBalances,
                query.SortBy,
                query.SortDescending);

            List<InventoryBalanceDetails> items = await InventoryBalanceDetails
                .QueryFrom(dbContext, inventoryBalances)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            return ServiceResult<ListResult<InventoryBalanceDetails>>
                .Success(new ListResult<InventoryBalanceDetails>(items, totalCount, skip, take));
        }

        private static IQueryable<InventoryBalance> ApplySorting(
            IQueryable<InventoryBalance> query,
            string? sortBy,
            bool sortDescending)
        {
            string normalizedSortBy = sortBy?.Trim().ToLowerInvariant() ?? "id";

            return normalizedSortBy switch
            {
                "id" => sortDescending
                    ? query.OrderByDescending(x => x.Id)
                    : query.OrderBy(x => x.Id),

                "quantity" => sortDescending
                    ? query.OrderByDescending(x => x.Quantity).ThenBy(x => x.Id)
                    : query.OrderBy(x => x.Quantity).ThenBy(x => x.Id),

                _ => query.OrderBy(x => x.Id)
            };
        }
    }
}
