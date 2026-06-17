using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Application;
using Myrmex.Core.Application.Queries;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Shared.Common;

namespace Myrmex.Modules.Wms.Catalog.Features.UnitsOfMeasure;

internal static class ListUnitsOfMeasure
{
    internal sealed record Query : ActiveListQuery, IQuery<ServiceResult<ListResult<UnitOfMeasureDetails>>>;

    internal sealed class Handler(WmsDbContext dbContext)
        : IQueryHandler<Query, ServiceResult<ListResult<UnitOfMeasureDetails>>>
    {
        public async Task<ServiceResult<ListResult<UnitOfMeasureDetails>>> HandleAsync(
            Query query,
            CancellationToken cancellationToken = default)
        {
            int skip = ListQuery.NormalizeSkip(query.Skip);
            int take = ListQuery.NormalizeTake(query.Take);

            IQueryable<UnitOfMeasure> queryable = dbContext.UnitsOfMeasure
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
                    (x.Symbol != null && x.Symbol.Contains(searchText)));
            }

            int totalCount = await queryable.CountAsync(cancellationToken);

            queryable = ApplySorting(queryable, query.SortBy, query.SortDescending);

            List<UnitOfMeasureDetails> items = await queryable
                .Skip(skip)
                .Take(take)
                .Select(UnitOfMeasureDetails.Projection)
                .ToListAsync(cancellationToken);

            return ServiceResult<ListResult<UnitOfMeasureDetails>>
                .Success(new ListResult<UnitOfMeasureDetails>(items, totalCount, skip, take));
        }

        private static IQueryable<UnitOfMeasure> ApplySorting(
            IQueryable<UnitOfMeasure> query,
            string? sortBy,
            bool sortDescending)
        {
            string normalizedSortBy = sortBy?.Trim().ToLowerInvariant() ?? "code";

            return normalizedSortBy switch
            {
                "code" => sortDescending ? query.OrderByDescending(x => x.Code) : query.OrderBy(x => x.Code),
                "name" => sortDescending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
                "isactive" => sortDescending ? query.OrderByDescending(x => x.IsActive) : query.OrderBy(x => x.IsActive),
                _ => query.OrderBy(x => x.Code)
            };
        }
    }
}
