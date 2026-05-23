using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;

namespace Myrmex.Modules.Wms.Topology.Features.Zones;

internal static class GetZoneById
{
    internal sealed record Query(Guid ZoneId) : IQuery<ServiceResult<ZoneDetails>>;

    internal sealed class Handler(WmsDbContext dbContext) : IQueryHandler<Query, ServiceResult<ZoneDetails>>
    {
        public async Task<ServiceResult<ZoneDetails>> HandleAsync(
            Query query,
            CancellationToken cancellationToken = default)
        {
            ZoneDetails? result = await dbContext.Zones
                .AsNoTracking()
                .Where(x => x.Id == query.ZoneId)
                .Select(ZoneDetails.Projection)
                .FirstOrDefaultAsync(cancellationToken);

            if (result is null)
            {
                return ServiceResult<ZoneDetails>.Fail(WmsErrors.Warehouse.NotFound);
            }

            return ServiceResult<ZoneDetails>.Success(result);
        }
    }
}