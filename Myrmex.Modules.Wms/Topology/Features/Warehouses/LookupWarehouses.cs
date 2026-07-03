using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;
using Myrmex.Shared.Wms.Topology;

namespace Myrmex.Modules.Wms.Topology.Features.Warehouses;

internal static class LookupWarehouses
{
    private const int DefaultTake = 20;
    private const int MaxTake = 20;

    internal sealed record Query : IQuery<ServiceResult<IReadOnlyList<WarehouseLookupItem>>>
    {
        public string? SearchText { get; init; }

        public int? Take { get; init; }

        public bool SelectableOnly { get; init; } = true;
    }

    internal sealed class Handler(WmsDbContext dbContext)
        : IQueryHandler<Query, ServiceResult<IReadOnlyList<WarehouseLookupItem>>>
    {
        public async Task<ServiceResult<IReadOnlyList<WarehouseLookupItem>>> HandleAsync(
            Query query,
            CancellationToken cancellationToken = default)
        {
            int take = NormalizeTake(query.Take);
            IQueryable<Warehouse> queryable = dbContext.Warehouses.AsNoTracking();

            if (query.SelectableOnly)
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

            List<WarehouseLookupItem> items = await queryable
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Code)
                .ThenBy(x => x.Id)
                .Take(take)
                .Select(x => new WarehouseLookupItem(
                    x.Id,
                    x.Code,
                    x.Name,
                    x.IsActive))
                .ToListAsync(cancellationToken);

            return ServiceResult<IReadOnlyList<WarehouseLookupItem>>.Success(items);
        }

        private static int NormalizeTake(int? take)
        {
            return take is null or <= 0
                ? DefaultTake
                : Math.Min(take.Value, MaxTake);
        }
    }
}
