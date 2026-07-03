using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Shared.Wms.Topology;

namespace Myrmex.Modules.Wms.Topology.Features.StorageLocations;

internal static class ListStorageLocationTypes
{
    internal sealed record Query(bool IncludeInactive = false)
        : IQuery<ServiceResult<IReadOnlyList<StorageLocationTypeDetails>>>;

    internal sealed class Handler(WmsDbContext dbContext)
        : IQueryHandler<Query, ServiceResult<IReadOnlyList<StorageLocationTypeDetails>>>
    {
        public async Task<ServiceResult<IReadOnlyList<StorageLocationTypeDetails>>> HandleAsync(
            Query query,
            CancellationToken cancellationToken = default)
        {
            var queryable = dbContext.StorageLocationTypes
                .AsNoTracking();

            if (!query.IncludeInactive)
            {
                queryable = queryable.Where(x => x.IsActive);
            }

            List<StorageLocationTypeDetails> items = await queryable
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Code)
                .Select(StorageLocationTypeDetailsMapping.Projection)
                .ToListAsync(cancellationToken);

            return ServiceResult<IReadOnlyList<StorageLocationTypeDetails>>.Success(items);
        }
    }
}
