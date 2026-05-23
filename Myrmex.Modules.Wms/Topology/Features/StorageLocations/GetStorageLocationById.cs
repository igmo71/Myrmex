using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;

namespace Myrmex.Modules.Wms.Topology.Features.StorageLocations;

internal static class GetStorageLocationById
{
    internal sealed record Query(Guid StorageLocationId) : IQuery<ServiceResult<StorageLocationDetails>>;

    internal sealed class Handler(WmsDbContext dbContext)
        : IQueryHandler<Query, ServiceResult<StorageLocationDetails>>
    {
        public async Task<ServiceResult<StorageLocationDetails>> HandleAsync(
            Query query,
            CancellationToken cancellationToken = default)
        {
            StorageLocationDetails? result = await dbContext.StorageLocations
                .AsNoTracking()
                .Where(x => x.Id == query.StorageLocationId)
                .Select(StorageLocationDetails.Projection)
                .FirstOrDefaultAsync(cancellationToken);

            if (result is null)
            {
                return ServiceResult<StorageLocationDetails>.Fail(WmsErrors.StorageLocation.NotFound);
            }

            return ServiceResult<StorageLocationDetails>.Success(result);
        }
    }
}