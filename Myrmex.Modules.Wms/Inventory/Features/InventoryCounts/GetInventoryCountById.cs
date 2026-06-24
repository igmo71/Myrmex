using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Application;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryCounts;
using Myrmex.Shared.Wms.Inventory;

namespace Myrmex.Modules.Wms.Inventory.Features.InventoryCounts;

internal static class GetInventoryCountById
{
    internal sealed record Query(Guid? InventoryCountId)
        : IQuery<ServiceResult<InventoryCountDetails>>;

    internal sealed class Handler(WmsDbContext dbContext)
        : IQueryHandler<Query, ServiceResult<InventoryCountDetails>>
    {
        public async Task<ServiceResult<InventoryCountDetails>> HandleAsync(
            Query query,
            CancellationToken cancellationToken = default)
        {
            if (!query.InventoryCountId.HasValue || query.InventoryCountId.Value == Guid.Empty)
            {
                return ServiceResult<InventoryCountDetails>.Invalid(
                    [DomainValidationFailure.Required<InventoryCount>(nameof(Query.InventoryCountId))]);
            }

            InventoryCountDetailsData? data = await dbContext.InventoryCounts
                .AsNoTracking()
                .Where(x => x.Id == query.InventoryCountId.Value)
                .ProjectDetailsData()
                .SingleOrDefaultAsync(cancellationToken);

            return data is null
                ? ServiceResult<InventoryCountDetails>.Fail(
                    ServiceError.NotFound<InventoryCount>(
                        "InventoryCount not found",
                        nameof(Query.InventoryCountId)))
                : ServiceResult<InventoryCountDetails>.Success(data.ToDetails());
        }
    }
}
