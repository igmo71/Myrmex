using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;

namespace Myrmex.Modules.Wms.Inventory.Features.InventoryBalances;

internal static class GetInventoryBalanceById
{
    internal sealed record Query(Guid InventoryBalanceId) : IQuery<ServiceResult<InventoryBalanceDetails>>;

    internal sealed class Handler(WmsDbContext dbContext)
        : IQueryHandler<Query, ServiceResult<InventoryBalanceDetails>>
    {
        public async Task<ServiceResult<InventoryBalanceDetails>> HandleAsync(
            Query query,
            CancellationToken cancellationToken = default)
        {
            IQueryable<InventoryBalance> inventoryBalanceQuery = dbContext.InventoryBalances
                .Where(x => x.Id == query.InventoryBalanceId);

            InventoryBalanceDetails? details = await InventoryBalanceDetails
                .QueryFrom(dbContext, inventoryBalanceQuery)
                .SingleOrDefaultAsync(cancellationToken);

            if (details is null)
            {
                return ServiceResult<InventoryBalanceDetails>.Fail(WmsErrors.InventoryBalance.NotFound);
            }

            return ServiceResult<InventoryBalanceDetails>.Success(details);
        }
    }
}
