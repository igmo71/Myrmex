using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;
using Myrmex.Shared.Wms.Inventory;

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
            InventoryBalanceDetails? details = await dbContext.InventoryBalances
                .AsNoTracking()
                .Where(x => x.Id == query.InventoryBalanceId)
                .ProjectDetails()
                .SingleOrDefaultAsync(cancellationToken);

            if (details is null)
            {
                return ServiceResult<InventoryBalanceDetails>.Fail(ServiceError.NotFound<InventoryBalance>());
            }

            return ServiceResult<InventoryBalanceDetails>.Success(details);
        }
    }
}
