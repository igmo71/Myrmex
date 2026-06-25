using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Application;
using Myrmex.Core.Application.Queries;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryCounts;
using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Inventory;

namespace Myrmex.Modules.Wms.Inventory.Features.InventoryCounts;

internal static class ListInventoryCounts
{
    internal sealed record Query
        : ListQuery, IQuery<ServiceResult<ListResult<InventoryCountListItem>>>
    {
        public Guid? WarehouseId { get; init; }
        public string? StatusText { get; init; }
        public InventoryCountStatus? Status { get; init; }
        public DateTimeOffset? CreatedFromUtc { get; init; }
        public DateTimeOffset? CreatedToUtc { get; init; }
    }

    internal sealed class Handler(WmsDbContext dbContext)
        : IQueryHandler<Query, ServiceResult<ListResult<InventoryCountListItem>>>
    {
        public async Task<ServiceResult<ListResult<InventoryCountListItem>>> HandleAsync(
            Query query,
            CancellationToken cancellationToken = default)
        {
            DomainValidationFailure[] failures = Validate(query);
            if (failures.Length > 0)
            {
                return ServiceResult<ListResult<InventoryCountListItem>>.Invalid(failures);
            }

            int skip = ListQuery.NormalizeSkip(query.Skip);
            int take = ListQuery.NormalizeTake(query.Take);
            IQueryable<InventoryCount> counts = dbContext.InventoryCounts
                .AsNoTracking()
                .ApplyFilters(query);

            int totalCount = await counts.CountAsync(cancellationToken);
            List<InventoryCountListItemData> data = await counts
                .ApplySorting(query.SortBy, query.SortDescending)
                .Skip(skip)
                .Take(take)
                .ProjectListItemData()
                .ToListAsync(cancellationToken);

            return ServiceResult<ListResult<InventoryCountListItem>>.Success(
                new ListResult<InventoryCountListItem>(
                    data.Select(x => x.ToListItem()).ToList(),
                    totalCount,
                    skip,
                    take));
        }

        private static DomainValidationFailure[] Validate(Query query)
        {
            List<DomainValidationFailure> failures = [];
            if (query.CreatedFromUtc.HasValue &&
                query.CreatedToUtc.HasValue &&
                query.CreatedFromUtc.Value > query.CreatedToUtc.Value)
            {
                failures.Add(
                    DomainValidationFailure.IncorrectState<Query>(
                        nameof(Query.CreatedFromUtc)));
            }

            if (IsUnsupportedStatus(query.StatusText))
            {
                failures.Add(
                    DomainValidationFailure.Unsupported<Query>(
                        nameof(Query.Status)));
            }

            return [.. failures];
        }
    }

    internal static InventoryCountStatus? ParseStatus(string? status)
    {
        return string.IsNullOrWhiteSpace(status)
            ? null
            : Enum.TryParse(
                status.Trim(),
                ignoreCase: false,
                out InventoryCountStatus parsed)
                ? parsed
                : null;
    }

    internal static bool IsUnsupportedStatus(string? status)
    {
        return !string.IsNullOrWhiteSpace(status) &&
               !Enum.TryParse(
                   status.Trim(),
                   ignoreCase: false,
                   out InventoryCountStatus _);
    }
}
