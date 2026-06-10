using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Application;
using Myrmex.Core.Application.Queries;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.SkuBarcodes;
using Myrmex.Modules.Wms.Infrastructure.Persistence;

namespace Myrmex.Modules.Wms.Catalog.Features.SkuBarcodes;

internal static class ListSkuBarcodes
{
    internal sealed record Query : ActiveListQuery, IQuery<ServiceResult<ListResult<SkuBarcodeDetails>>>
    {
        public Guid? StockKeepingUnitId { get; init; }
    }

    internal sealed class Handler(WmsDbContext dbContext)
        : IQueryHandler<Query, ServiceResult<ListResult<SkuBarcodeDetails>>>
    {
        public async Task<ServiceResult<ListResult<SkuBarcodeDetails>>> HandleAsync(
            Query query,
            CancellationToken cancellationToken = default)
        {
            int skip = ListQuery.NormalizeSkip(query.Skip);
            int take = ListQuery.NormalizeTake(query.Take);

            IQueryable<SkuBarcode> queryable = dbContext.SkuBarcodes
                .AsNoTracking();

            if (!query.IncludeInactive)
            {
                queryable = queryable.Where(x => x.IsActive);
            }

            if (query.StockKeepingUnitId.HasValue)
            {
                queryable = queryable.Where(x => x.StockKeepingUnitId == query.StockKeepingUnitId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.SearchText))
            {
                string searchText = query.SearchText.Trim();

                queryable = queryable.Where(x => x.Value.Contains(searchText));
            }

            int totalCount = await queryable.CountAsync(cancellationToken);

            queryable = ApplySorting(queryable, query.SortBy, query.SortDescending);

            List<SkuBarcodeDetails> items = await queryable
                .Skip(skip)
                .Take(take)
                .Select(SkuBarcodeDetails.Projection)
                .ToListAsync(cancellationToken);

            return ServiceResult<ListResult<SkuBarcodeDetails>>
                .Success(new ListResult<SkuBarcodeDetails>(items, totalCount, skip, take));
        }

        private static IQueryable<SkuBarcode> ApplySorting(
            IQueryable<SkuBarcode> query,
            string? sortBy,
            bool sortDescending)
        {
            string normalizedSortBy = sortBy?.Trim().ToLowerInvariant() ?? "value";

            return normalizedSortBy switch
            {
                "value" => sortDescending ? query.OrderByDescending(x => x.Value) : query.OrderBy(x => x.Value),
                "symbology" => sortDescending ? query.OrderByDescending(x => x.Symbology) : query.OrderBy(x => x.Symbology),
                "isactive" => sortDescending ? query.OrderByDescending(x => x.IsActive) : query.OrderBy(x => x.IsActive),
                _ => query.OrderBy(x => x.Value)
            };
        }
    }
}
