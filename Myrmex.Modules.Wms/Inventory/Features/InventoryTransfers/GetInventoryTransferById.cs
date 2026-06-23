using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Application;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryTransfers;
using Myrmex.Shared.Wms.Inventory;

namespace Myrmex.Modules.Wms.Inventory.Features.InventoryTransfers;

internal static class GetInventoryTransferById
{
    internal sealed record Query(Guid? TransferId) : IQuery<ServiceResult<InventoryTransferDetails>>;

    internal sealed class Handler(WmsDbContext dbContext)
        : IQueryHandler<Query, ServiceResult<InventoryTransferDetails>>
    {
        public async Task<ServiceResult<InventoryTransferDetails>> HandleAsync(
            Query query,
            CancellationToken cancellationToken = default)
        {
            if (!query.TransferId.HasValue || query.TransferId.Value == Guid.Empty)
            {
                return ServiceResult<InventoryTransferDetails>.Invalid(
                    [DomainValidationFailure.Required<InventoryTransfer>(nameof(Query.TransferId))]);
            }

            InventoryTransferDetailsData? detailsData = await dbContext.InventoryTransfers
                .AsNoTracking()
                .Where(x => x.Id == query.TransferId.Value)
                .ProjectDetailsData()
                .SingleOrDefaultAsync(cancellationToken);

            return detailsData is null
                ? ServiceResult<InventoryTransferDetails>.Fail(
                    ServiceError.NotFound<InventoryTransfer>("InventoryTransfer not found", nameof(Query.TransferId)))
                : ServiceResult<InventoryTransferDetails>.Success(detailsData.ToDetails());
        }
    }
}
