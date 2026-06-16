using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Infrastructure.Persistence;

namespace Myrmex.Modules.Wms.Catalog.Features.StockKeepingUnits;

internal static class GetStockKeepingUnitById
{
    internal sealed record Query(Guid StockKeepingUnitId) : IQuery<ServiceResult<StockKeepingUnitDetails>>;

    internal sealed class Handler(WmsDbContext dbContext) : IQueryHandler<Query, ServiceResult<StockKeepingUnitDetails>>
    {
        public async Task<ServiceResult<StockKeepingUnitDetails>> HandleAsync(
            Query query,
            CancellationToken cancellationToken = default)
        {
            StockKeepingUnitDetails? result = await dbContext.StockKeepingUnits
                .AsNoTracking()
                .Where(x => x.Id == query.StockKeepingUnitId)
                .Select(StockKeepingUnitDetails.Projection)
                .FirstOrDefaultAsync(cancellationToken);

            if (result is null)
            {
                return ServiceResult<StockKeepingUnitDetails>.Fail(ServiceError.NotFound<StockKeepingUnit>());
            }

            return ServiceResult<StockKeepingUnitDetails>.Success(result);
        }
    }
}
