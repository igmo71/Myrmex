using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;
using Myrmex.Shared.Wms.Inventory;

namespace Myrmex.Modules.Wms.Inventory.Features.InventoryBalances;

internal static class GetInventoryBalanceBySkuAndStorageLocation
{
    internal sealed record Query(
        Guid StockKeepingUnitId,
        Guid StorageLocationId)
        : IQuery<ServiceResult<InventoryBalanceDetails>>;

    internal sealed class Handler(WmsDbContext dbContext)
        : IQueryHandler<Query, ServiceResult<InventoryBalanceDetails>>
    {
        public async Task<ServiceResult<InventoryBalanceDetails>> HandleAsync(
            Query query,
            CancellationToken cancellationToken = default)
        {
            InventoryBalanceDetailsData? detailsData = await dbContext.InventoryBalances
                .AsNoTracking()
                .Where(x =>
                    x.StockKeepingUnitId == query.StockKeepingUnitId &&
                    x.StorageLocationId == query.StorageLocationId)
                .ProjectDetailsData()
                .SingleOrDefaultAsync(cancellationToken);

            if (detailsData is null)
            {
                return ServiceResult<InventoryBalanceDetails>.Fail(
                    ServiceError.NotFound<InventoryBalance>(
                        "Inventory balance not found."));
            }

            return ServiceResult<InventoryBalanceDetails>.Success(
                detailsData.ToDetails());
        }
    }
}
