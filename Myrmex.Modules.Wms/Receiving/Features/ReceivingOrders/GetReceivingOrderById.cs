using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Application;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Receiving.Domain.ReceivingOrders;
using Myrmex.Shared.Wms.Receiving;

namespace Myrmex.Modules.Wms.Receiving.Features.ReceivingOrders;

internal static class GetReceivingOrderById
{
    internal sealed record Query(Guid? ReceivingOrderId)
        : IQuery<ServiceResult<ReceivingOrderDetails>>;

    internal sealed class Handler(WmsDbContext dbContext)
        : IQueryHandler<Query, ServiceResult<ReceivingOrderDetails>>
    {
        public async Task<ServiceResult<ReceivingOrderDetails>> HandleAsync(
            Query query,
            CancellationToken cancellationToken = default)
        {
            if (!query.ReceivingOrderId.HasValue || query.ReceivingOrderId.Value == Guid.Empty)
            {
                return ServiceResult<ReceivingOrderDetails>.Invalid([
                    DomainValidationFailure.Required<ReceivingOrder>(nameof(Query.ReceivingOrderId))]);
            }

            ReceivingOrderDetailsData? data = await dbContext.ReceivingOrders
                .AsNoTracking()
                .Where(x => x.Id == query.ReceivingOrderId.Value)
                .ProjectDetailsData()
                .SingleOrDefaultAsync(cancellationToken);

            return data is null
                ? ServiceResult<ReceivingOrderDetails>.Fail(
                    ServiceError.NotFound<ReceivingOrder>(
                        "ReceivingOrder not found",
                        nameof(Query.ReceivingOrderId)))
                : ServiceResult<ReceivingOrderDetails>.Success(data.ToDetails());
        }
    }
}
