using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Application;
using Myrmex.Core.Application.Queries;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;
using Myrmex.Shared.Wms.Inventory;

namespace Myrmex.Modules.Wms.Inventory.Features.InventoryBalances;

internal static class ListInventoryBalances
{
    internal sealed record Query : ListQuery, IQuery<ServiceResult<ListResult<InventoryBalanceDetails>>>
    {
        public Guid? StockKeepingUnitId { get; init; }
        public Guid? StorageLocationId { get; init; }
        public Guid? WarehouseId { get; init; }
    }

    internal sealed class Handler(WmsDbContext dbContext) : IQueryHandler<Query, ServiceResult<ListResult<InventoryBalanceDetails>>>
    {
        public async Task<ServiceResult<ListResult<InventoryBalanceDetails>>> HandleAsync(
            Query query,
            CancellationToken cancellationToken = default)
        {
            int skip = ListQuery.NormalizeSkip(query.Skip);
            int take = ListQuery.NormalizeTake(query.Take);

            IQueryable<InventoryBalance> inventoryBalances = dbContext.InventoryBalances
                .AsNoTracking()
                .ApplyFilters(query);

            int totalCount = await inventoryBalances
                .CountAsync(cancellationToken);

            List<InventoryBalanceDetails> items = await inventoryBalances
                .ApplySorting(query.SortBy, query.SortDescending)
                .Skip(skip)
                .Take(take)
                .ProjectDetails()
                .ToListAsync(cancellationToken);

            return ServiceResult<ListResult<InventoryBalanceDetails>>
                .Success(new ListResult<InventoryBalanceDetails>(items, totalCount, skip, take));
        }
    }
}
