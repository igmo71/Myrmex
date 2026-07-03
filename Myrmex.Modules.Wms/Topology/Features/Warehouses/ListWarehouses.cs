using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Application;
using Myrmex.Core.Application.Queries;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;
using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Topology;

namespace Myrmex.Modules.Wms.Topology.Features.Warehouses;

internal static class ListWarehouses
{
    internal sealed record Query : ActiveListQuery, IQuery<ServiceResult<ListResult<WarehouseDetails>>>;

    internal sealed class Handler(WmsDbContext dbContext) : IQueryHandler<Query, ServiceResult<ListResult<WarehouseDetails>>>
    {
        public async Task<ServiceResult<ListResult<WarehouseDetails>>> HandleAsync(Query query, CancellationToken cancellationToken = default)
        {
            int skip = ListQuery.NormalizeSkip(query.Skip);
            int take = ListQuery.NormalizeTake(query.Take);

            IQueryable<Warehouse> queryable = dbContext.Warehouses
                .AsNoTracking();

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

            List<WarehouseDetails> items = await queryable
                .Skip(skip)
                .Take(take)
                .Select(WarehouseDetailsMapping.Projection)
                .ToListAsync(cancellationToken);

            return ServiceResult<ListResult<WarehouseDetails>>
                .Success(new ListResult<WarehouseDetails>(items, totalCount, skip, take));
        }

        private static IQueryable<Warehouse> ApplySorting(IQueryable<Warehouse> query, string? sortBy, bool sortDescending)
        {
            string normalizedSortBy = sortBy?.Trim().ToLowerInvariant() ?? "name";

            return normalizedSortBy switch
            {
                "code" => sortDescending
                    ? query.OrderByDescending(x => x.Code).ThenBy(x => x.Id)
                    : query.OrderBy(x => x.Code).ThenBy(x => x.Id),
                "name" => sortDescending
                    ? query.OrderByDescending(x => x.Name).ThenBy(x => x.Id)
                    : query.OrderBy(x => x.Name).ThenBy(x => x.Id),
                "createdatutc" => sortDescending
                    ? query.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id)
                    : query.OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id),
                "updatedatutc" => sortDescending
                    ? query.OrderByDescending(x => x.UpdatedAtUtc).ThenBy(x => x.Id)
                    : query.OrderBy(x => x.UpdatedAtUtc).ThenBy(x => x.Id),
                "isactive" => sortDescending
                    ? query.OrderByDescending(x => x.IsActive).ThenBy(x => x.Id)
                    : query.OrderBy(x => x.IsActive).ThenBy(x => x.Id),
                _ => query.OrderBy(x => x.Name).ThenBy(x => x.Id)
            };
        }
    }
}
