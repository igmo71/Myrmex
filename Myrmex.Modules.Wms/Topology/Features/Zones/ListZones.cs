using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Application;
using Myrmex.Core.Application.Queries;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Topology.Domain.Zones;

namespace Myrmex.Modules.Wms.Topology.Features.Zones;

internal static class ListZones
{
    internal sealed record Query(Guid WarehouseId) : ActiveListQuery, IQuery<ServiceResult<ListResult<ZoneDetails>>>;

    internal sealed class Handler(WmsDbContext dbContext) : IQueryHandler<Query, ServiceResult<ListResult<ZoneDetails>>>
    {
        public async Task<ServiceResult<ListResult<ZoneDetails>>> HandleAsync(
            Query query,
            CancellationToken cancellationToken = default)
        {
            int skip = ListQuery.NormalizeSkip(query.Skip);
            int take = ListQuery.NormalizeTake(query.Take);

            bool warehouseExists = await dbContext.Warehouses
                .AnyAsync(x => x.Id == query.WarehouseId, cancellationToken);

            if (!warehouseExists)
            {
                return ServiceResult<ListResult<ZoneDetails>>.Fail(ServiceError.NotFound<Zone>("Warehouse not found", "Warehouse"));
            }

            IQueryable<Zone> queryable = dbContext.Zones
                .AsNoTracking()
                .Where(x => x.WarehouseId == query.WarehouseId);

            if (!query.IncludeInactive)
            {
                queryable = queryable.Where(x => x.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(query.SearchText))
            {
                string searchText = query.SearchText.Trim();

                queryable = queryable.Where(x =>
                    x.Code.Contains(searchText) ||
                    x.Name.Contains(searchText) ||
                    (x.Description != null && x.Description.Contains(searchText)));
            }

            int totalCount = await queryable.CountAsync(cancellationToken);

            queryable = ApplySorting(queryable, query.SortBy, query.SortDescending);

            List<ZoneDetails> items = await queryable
                .Skip(skip)
                .Take(take)
                .Select(ZoneDetails.Projection)
                .ToListAsync(cancellationToken);

            return ServiceResult<ListResult<ZoneDetails>>
                .Success(new ListResult<ZoneDetails>(items, totalCount, skip, take));
        }

        private static IQueryable<Zone> ApplySorting(
            IQueryable<Zone> query,
            string? sortBy,
            bool sortDescending)
        {
            string normalizedSortBy = sortBy?.Trim().ToLowerInvariant() ?? "code";

            return normalizedSortBy switch
            {
                "code" => sortDescending ? query.OrderByDescending(x => x.Code) : query.OrderBy(x => x.Code),
                "name" => sortDescending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
                "createdatutc" => sortDescending ? query.OrderByDescending(x => x.CreatedAtUtc) : query.OrderBy(x => x.CreatedAtUtc),
                "updatedatutc" => sortDescending ? query.OrderByDescending(x => x.UpdatedAtUtc) : query.OrderBy(x => x.UpdatedAtUtc),
                "isactive" => sortDescending ? query.OrderByDescending(x => x.IsActive) : query.OrderBy(x => x.IsActive),
                _ => query.OrderBy(x => x.Code)
            };
        }
    }
}