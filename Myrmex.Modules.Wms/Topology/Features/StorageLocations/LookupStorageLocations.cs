using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;
using Myrmex.Shared.Wms.Topology;

namespace Myrmex.Modules.Wms.Topology.Features.StorageLocations;

internal static class LookupStorageLocations
{
    private const int DefaultTake = 20;
    private const int MaxTake = 20;
    private const string InternalTransitStorageLocationTypeCode = "INTERNAL_TRANSIT";
    private const string ExternalTransitStorageLocationTypeCode = "EXTERNAL_TRANSIT";

    internal sealed record Query : IQuery<ServiceResult<IReadOnlyList<StorageLocationLookupItem>>>
    {
        public Guid WarehouseId { get; init; }

        public string? SearchText { get; init; }

        public int? Take { get; init; }

        public bool SelectableOnly { get; init; } = true;

        public string? StorageLocationTypeCode { get; init; }

        public bool ExcludeTransitTypes { get; init; }
    }

    internal sealed class Handler(WmsDbContext dbContext)
        : IQueryHandler<Query, ServiceResult<IReadOnlyList<StorageLocationLookupItem>>>
    {
        public async Task<ServiceResult<IReadOnlyList<StorageLocationLookupItem>>> HandleAsync(
            Query query,
            CancellationToken cancellationToken = default)
        {
            int take = NormalizeTake(query.Take);

            bool warehouseExists = await dbContext.Warehouses
                .AnyAsync(x => x.Id == query.WarehouseId, cancellationToken);

            if (!warehouseExists)
            {
                return ServiceResult<IReadOnlyList<StorageLocationLookupItem>>
                    .Fail(ServiceError.NotFound<StorageLocation>("Warehouse not found", "Warehouse"));
            }

            IQueryable<StorageLocation> queryable = dbContext.StorageLocations
                .AsNoTracking()
                .Where(x => x.WarehouseId == query.WarehouseId);

            if (query.SelectableOnly)
            {
                queryable = queryable.WhereSelectable();
            }

            if (!string.IsNullOrWhiteSpace(query.StorageLocationTypeCode))
            {
                string storageLocationTypeCode = query.StorageLocationTypeCode.Trim();

                queryable = queryable.Where(x =>
                    x.StorageLocationType.Code == storageLocationTypeCode);
            }

            if (query.ExcludeTransitTypes)
            {
                queryable = queryable.Where(x =>
                    x.StorageLocationType.Code != InternalTransitStorageLocationTypeCode &&
                    x.StorageLocationType.Code != ExternalTransitStorageLocationTypeCode);
            }

            if (!string.IsNullOrWhiteSpace(query.SearchText))
            {
                string searchText = query.SearchText.Trim();

                queryable = queryable.Where(x =>
                    x.Code.Contains(searchText) ||
                    x.Name.Contains(searchText));
            }

            List<StorageLocationLookupItem> items = await queryable
                .OrderBy(x => x.Code)
                .ThenBy(x => x.Name)
                .ThenBy(x => x.Id)
                .Take(take)
                .Select(x => new StorageLocationLookupItem(
                    x.Id,
                    x.WarehouseId,
                    x.Code,
                    x.Name,
                    x.IsActive))
                .ToListAsync(cancellationToken);

            return ServiceResult<IReadOnlyList<StorageLocationLookupItem>>.Success(items);
        }

        private static int NormalizeTake(int? take)
        {
            return take is null or <= 0
                ? DefaultTake
                : Math.Min(take.Value, MaxTake);
        }
    }
}
