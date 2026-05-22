using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Application;
using Myrmex.Core.Application.Queries;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;

namespace Myrmex.Modules.Wms.Topology.Features.Warehouses;

internal static class ListWarehouses
{
    private const int DefaultTake = 20;
    private const int MaxTake = 200;

    internal sealed record Query : ListQuery, IQuery<ServiceResult<ListResult<Item>>>;

    internal sealed record Item(
        Guid Id,
        string Code,
        string Name,
        string? Description,
        bool IsActive,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? UpdatedAtUtc);

    internal sealed class Handler(WmsDbContext dbContext) : IQueryHandler<Query, ServiceResult<ListResult<Item>>>
    {
        public async Task<ServiceResult<ListResult<Item>>> HandleAsync(Query query, CancellationToken cancellationToken = default)
        {
            int skip = Math.Max(0, query.Skip);
            int take = query.Take <= 0 ? DefaultTake : Math.Min(query.Take, MaxTake);

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

            queryable = ApplySorting(queryable, query.SortBy, query.SortDescending);

            int totalCount = await queryable.CountAsync(cancellationToken);

            List<Item> items = await queryable
                .Skip(skip)
                .Take(take)
                .Select(x => new Item(
                    x.Id,
                    x.Code,
                    x.Name,
                    x.Description,
                    x.IsActive,
                    x.CreatedAtUtc,
                    x.UpdatedAtUtc))
                .ToListAsync(cancellationToken);

            return ServiceResult<ListResult<Item>>
                .Success(new ListResult<Item>(items, totalCount, skip, take));
        }

        private static IQueryable<Warehouse> ApplySorting(IQueryable<Warehouse> query, string? sortBy, bool sortDescending)
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
