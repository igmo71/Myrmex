using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;

namespace Myrmex.Modules.Wms.Topology.Features.StorageLocations;

internal static class ListStorageLocationStatuses
{
    internal sealed record Query(bool IncludeInactive = false)
        : IQuery<ServiceResult<IReadOnlyList<StorageLocationStatusDetails>>>;

    internal sealed class Handler(WmsDbContext dbContext)
        : IQueryHandler<Query, ServiceResult<IReadOnlyList<StorageLocationStatusDetails>>>
    {
        public async Task<ServiceResult<IReadOnlyList<StorageLocationStatusDetails>>> HandleAsync(
            Query query,
            CancellationToken cancellationToken = default)
        {
            var queryable = dbContext.StorageLocationStatuses
                .AsNoTracking();

            if (!query.IncludeInactive)
            {
                queryable = queryable.Where(x => x.IsActive);
            }

            List<StorageLocationStatusDetails> items = await queryable
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Code)
                .Select(StorageLocationStatusDetails.Projection)
                .ToListAsync(cancellationToken);

            return ServiceResult<IReadOnlyList<StorageLocationStatusDetails>>.Success(items);
        }
    }
}