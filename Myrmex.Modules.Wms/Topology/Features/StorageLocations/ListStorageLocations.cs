using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Application;
using Myrmex.Core.Application.Queries;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;
using Myrmex.Shared.Common;

namespace Myrmex.Modules.Wms.Topology.Features.StorageLocations;

internal static class ListStorageLocations
{
    internal sealed record Query : ActiveListQuery, IQuery<ServiceResult<ListResult<StorageLocationDetails>>>
    {
        public Guid? WarehouseId { get; init; }
        public Guid? ZoneId { get; init; }
    }

    internal sealed class Handler(WmsDbContext dbContext)
        : IQueryHandler<Query, ServiceResult<ListResult<StorageLocationDetails>>>
    {
        public async Task<ServiceResult<ListResult<StorageLocationDetails>>> HandleAsync(
            Query query,
            CancellationToken cancellationToken = default)
        {
            int skip = ListQuery.NormalizeSkip(query.Skip);
            int take = ListQuery.NormalizeTake(query.Take);

            if (query.WarehouseId.HasValue)
            {
                bool warehouseExists = await dbContext.Warehouses
                    .AnyAsync(x => x.Id == query.WarehouseId.Value, cancellationToken);

                if (!warehouseExists)
                {
                    return ServiceResult<ListResult<StorageLocationDetails>>.Fail(ServiceError.NotFound<StorageLocation>("Warehouse not found", "Warehouse"));
                }
            }

            if (query.ZoneId.HasValue)
            {
                var zone = await dbContext.Zones
                    .AsNoTracking()
                    .Where(x => x.Id == query.ZoneId.Value)
                    .Select(x => new { x.Id, x.WarehouseId })
                    .FirstOrDefaultAsync(cancellationToken);

                if (zone is null)
                {
                    return ServiceResult<ListResult<StorageLocationDetails>>.Fail(ServiceError.NotFound<StorageLocation>("Zone not found", "Zone"));
                }

                if (query.WarehouseId.HasValue && zone.WarehouseId != query.WarehouseId.Value)
                {
                    return ServiceResult<ListResult<StorageLocationDetails>>.Fail(ServiceError.Conflict<StorageLocation>(message: "Zone - Warehouse Mismatch"));
                }
            }

            IQueryable<StorageLocation> queryable = dbContext.StorageLocations
                .AsNoTracking();

            if (query.WarehouseId.HasValue)
            {
                queryable = queryable.Where(x => x.WarehouseId == query.WarehouseId.Value);
            }

            if (query.ZoneId.HasValue)
            {
                queryable = queryable.Where(x => x.ZoneId == query.ZoneId.Value);
            }

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

            List<StorageLocationDetails> items = await queryable
                .Skip(skip)
                .Take(take)
                .Select(StorageLocationDetails.Projection)
                .ToListAsync(cancellationToken);

            return ServiceResult<ListResult<StorageLocationDetails>>.Success(new ListResult<StorageLocationDetails>(items, totalCount, skip, take));
        }

        private static IQueryable<StorageLocation> ApplySorting(
            IQueryable<StorageLocation> query,
            string? sortBy,
            bool sortDescending)
        {
            string normalizedSortBy = sortBy?.Trim().ToLowerInvariant() ?? "code";

            return normalizedSortBy switch
            {
                "code" => sortDescending ? query.OrderByDescending(x => x.Code) : query.OrderBy(x => x.Code),
                "name" => sortDescending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
                "ispickable" => sortDescending ? query.OrderByDescending(x => x.IsPickable) : query.OrderBy(x => x.IsPickable),
                "createdatutc" => sortDescending ? query.OrderByDescending(x => x.CreatedAtUtc) : query.OrderBy(x => x.CreatedAtUtc),
                "updatedatutc" => sortDescending ? query.OrderByDescending(x => x.UpdatedAtUtc) : query.OrderBy(x => x.UpdatedAtUtc),
                "isactive" => sortDescending ? query.OrderByDescending(x => x.IsActive) : query.OrderBy(x => x.IsActive),
                _ => query.OrderBy(x => x.Code)
            };
        }
    }
}