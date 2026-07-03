using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Shared.Wms.Catalog;

namespace Myrmex.Modules.Wms.Catalog.Features.UnitsOfMeasure;

internal static class GetUnitOfMeasureById
{
    internal sealed record Query(Guid UnitOfMeasureId) : IQuery<ServiceResult<UnitOfMeasureDetails>>;

    internal sealed class Handler(WmsDbContext dbContext) : IQueryHandler<Query, ServiceResult<UnitOfMeasureDetails>>
    {
        public async Task<ServiceResult<UnitOfMeasureDetails>> HandleAsync(
            Query query,
            CancellationToken cancellationToken = default)
        {
            UnitOfMeasureDetails? result = await dbContext.UnitsOfMeasure
                .AsNoTracking()
                .Where(x => x.Id == query.UnitOfMeasureId)
                .Select(UnitOfMeasureDetailsMapping.Projection)
                .FirstOrDefaultAsync(cancellationToken);

            if (result is null)
            {
                return ServiceResult<UnitOfMeasureDetails>.Fail(ServiceError.NotFound<UnitOfMeasure>());
            }

            return ServiceResult<UnitOfMeasureDetails>.Success(result);
        }
    }
}
