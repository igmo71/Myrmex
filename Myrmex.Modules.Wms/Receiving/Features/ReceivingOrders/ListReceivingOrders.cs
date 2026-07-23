using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Application;
using Myrmex.Core.Application.Queries;
using Myrmex.Core.Domain;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Receiving.Domain.ReceivingOrders;
using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Receiving;

namespace Myrmex.Modules.Wms.Receiving.Features.ReceivingOrders;

internal static class ListReceivingOrders
{
    private static readonly HashSet<string> SupportedSorts =
    [
        ReceivingOrderSortBy.Number,
        ReceivingOrderSortBy.Status,
        ReceivingOrderSortBy.WarehouseCode,
        ReceivingOrderSortBy.CreatedAtUtc,
        ReceivingOrderSortBy.StartedAtUtc,
        ReceivingOrderSortBy.CompletedAtUtc,
        ReceivingOrderSortBy.TotalPlannedQuantity
    ];

    internal sealed record Query
        : ListQuery, IQuery<ServiceResult<ListResult<ReceivingOrderListItem>>>
    {
        public string? NormalizedSearchText { get; init; }
        public Guid? WarehouseId { get; init; }
        public string? StatusText { get; init; }
        public ReceivingOrderStatus? Status { get; init; }
    }

    internal sealed class Handler(WmsDbContext dbContext)
        : IQueryHandler<Query, ServiceResult<ListResult<ReceivingOrderListItem>>>
    {
        public async Task<ServiceResult<ListResult<ReceivingOrderListItem>>> HandleAsync(
            Query query,
            CancellationToken cancellationToken = default)
        {
            DomainValidationFailure[] failures = Validate(query);
            if (failures.Length > 0)
            {
                return ServiceResult<ListResult<ReceivingOrderListItem>>.Invalid(failures);
            }

            int skip = ListQuery.NormalizeSkip(query.Skip);
            int take = ListQuery.NormalizeTake(query.Take);
            IQueryable<ReceivingOrder> orders = dbContext.ReceivingOrders
                .AsNoTracking()
                .ApplyFilters(query);

            int totalCount = await orders.CountAsync(cancellationToken);
            List<ReceivingOrderListItemData> data = await orders
                .ApplySorting(NormalizeSort(query.SortBy), query.SortDescending)
                .Skip(skip)
                .Take(take)
                .ProjectListItemData()
                .ToListAsync(cancellationToken);

            return ServiceResult<ListResult<ReceivingOrderListItem>>.Success(
                new(
                    data.Select(item => item.ToListItem()).ToList(),
                    totalCount,
                    skip,
                    take));
        }
    }

    internal static string? NormalizeSearchText(string? searchText)
    {
        string normalized = DomainText.NormalizeCode(searchText);
        return normalized.Length == 0 ? null : normalized;
    }

    internal static ReceivingOrderStatus? ParseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        return Enum.TryParse(
                   status.Trim(),
                   ignoreCase: false,
                   out ReceivingOrderStatus parsed) &&
               Enum.IsDefined(parsed)
            ? parsed
            : null;
    }

    private static DomainValidationFailure[] Validate(Query query)
    {
        List<DomainValidationFailure> failures = [];

        if (!string.IsNullOrWhiteSpace(query.StatusText) && !query.Status.HasValue)
        {
            failures.Add(DomainValidationFailure.Unsupported<Query>(nameof(Query.Status)));
        }

        string? sortBy = NormalizeSort(query.SortBy);
        if (sortBy is not null && !SupportedSorts.Contains(sortBy))
        {
            failures.Add(DomainValidationFailure.Unsupported<Query>(nameof(Query.SortBy)));
        }

        return [.. failures];
    }

    private static string? NormalizeSort(string? sortBy) =>
        string.IsNullOrWhiteSpace(sortBy) ? null : sortBy.Trim();
}
