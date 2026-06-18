using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Shared.Wms.Catalog;

namespace Myrmex.Modules.Wms.Catalog.Features.StockKeepingUnits;

internal static class LookupStockKeepingUnits
{
    private const int DefaultTake = 20;
    private const int MaxTake = 20;

    internal sealed record Query : IQuery<ServiceResult<IReadOnlyList<StockKeepingUnitLookupItem>>>
    {
        public string? SearchText { get; init; }

        public int? Take { get; init; }

        public bool SelectableOnly { get; init; } = true;
    }

    internal sealed class Handler(WmsDbContext dbContext)
        : IQueryHandler<Query, ServiceResult<IReadOnlyList<StockKeepingUnitLookupItem>>>
    {
        public async Task<ServiceResult<IReadOnlyList<StockKeepingUnitLookupItem>>> HandleAsync(
            Query query,
            CancellationToken cancellationToken = default)
        {
            int take = NormalizeTake(query.Take);

            IQueryable<StockKeepingUnit> queryable = dbContext.StockKeepingUnits
                .AsNoTracking();

            if (query.SelectableOnly)
            {
                queryable = queryable.Where(x => x.IsActive && x.BaseUnitOfMeasure.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(query.SearchText))
            {
                string searchText = query.SearchText.Trim();

                queryable = queryable.Where(x =>
                    x.Code.Contains(searchText) ||
                    x.Name.Contains(searchText));
            }

            List<StockKeepingUnitLookupItem> items = await queryable
                .OrderBy(x => x.Code)
                .ThenBy(x => x.Name)
                .ThenBy(x => x.Id)
                .Take(take)
                .Select(x => new StockKeepingUnitLookupItem(
                    x.Id,
                    x.Code,
                    x.Name,
                    x.BaseUnitOfMeasureId,
                    x.BaseUnitOfMeasure.Code,
                    x.BaseUnitOfMeasure.Symbol,
                    x.IsActive,
                    x.BaseUnitOfMeasure.IsActive))
                .ToListAsync(cancellationToken);

            return ServiceResult<IReadOnlyList<StockKeepingUnitLookupItem>>.Success(items);
        }

        private static int NormalizeTake(int? take)
        {
            return take is null or <= 0
                ? DefaultTake
                : Math.Min(take.Value, MaxTake);
        }
    }
}
