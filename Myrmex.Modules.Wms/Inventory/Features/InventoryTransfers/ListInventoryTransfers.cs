using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Application;
using Myrmex.Core.Application.Queries;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryTransfers;
using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Inventory;

namespace Myrmex.Modules.Wms.Inventory.Features.InventoryTransfers;

internal static class ListInventoryTransfers
{
    internal sealed record Query : ListQuery, IQuery<ServiceResult<ListResult<InventoryTransferListItem>>>
    {
        public Guid? WarehouseId { get; init; }
        public string? StatusText { get; init; }
        public InventoryTransferStatus? Status { get; init; }
        public DateTimeOffset? CreatedFromUtc { get; init; }
        public DateTimeOffset? CreatedToUtc { get; init; }
        public string? TransferCode { get; init; }
        public Guid? SourceStorageLocationId { get; init; }
        public Guid? DestinationStorageLocationId { get; init; }
        public Guid? StockKeepingUnitId { get; init; }
        public bool? HasTransitLocation { get; init; }
    }

    internal sealed class Handler(WmsDbContext dbContext)
        : IQueryHandler<Query, ServiceResult<ListResult<InventoryTransferListItem>>>
    {
        public async Task<ServiceResult<ListResult<InventoryTransferListItem>>> HandleAsync(
            Query query,
            CancellationToken cancellationToken = default)
        {
            DomainValidationFailure[] validationFailures = ValidateQuery(query);

            if (validationFailures.Length > 0)
            {
                return ServiceResult<ListResult<InventoryTransferListItem>>.Invalid(validationFailures);
            }

            int skip = ListQuery.NormalizeSkip(query.Skip);
            int take = ListQuery.NormalizeTake(query.Take);

            IQueryable<InventoryTransfer> transfers = dbContext.InventoryTransfers
                .AsNoTracking()
                .ApplyFilters(query);

            int totalCount = await transfers.CountAsync(cancellationToken);

            List<InventoryTransferListItemData> itemData = await transfers
                .ApplySorting(query.SortBy, query.SortDescending)
                .Skip(skip)
                .Take(take)
                .ProjectListItemData()
                .ToListAsync(cancellationToken);

            List<InventoryTransferListItem> items = itemData
                .Select(x => x.ToListItem())
                .ToList();

            return ServiceResult<ListResult<InventoryTransferListItem>>
                .Success(new ListResult<InventoryTransferListItem>(items, totalCount, skip, take));
        }

        private static DomainValidationFailure[] ValidateQuery(Query query)
        {
            List<DomainValidationFailure> failures = [];

            if (query.CreatedFromUtc.HasValue &&
                query.CreatedToUtc.HasValue &&
                query.CreatedFromUtc.Value > query.CreatedToUtc.Value)
            {
                failures.Add(DomainValidationFailure.IncorrectState<Query>(nameof(Query.CreatedFromUtc)));
            }

            if (IsUnsupportedStatus(query.StatusText))
            {
                failures.Add(DomainValidationFailure.Unsupported<Query>(nameof(Query.Status)));
            }

            return [.. failures];
        }
    }

    internal static InventoryTransferStatus? ParseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        return Enum.TryParse(status.Trim(), ignoreCase: false, out InventoryTransferStatus parsed)
            ? parsed
            : null;
    }

    internal static bool IsUnsupportedStatus(string? status)
    {
        return !string.IsNullOrWhiteSpace(status) &&
               !Enum.TryParse(status.Trim(), ignoreCase: false, out InventoryTransferStatus _);
    }
}
