using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Application;
using Myrmex.Core.Application.Queries;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryTransactions;
using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Inventory;

namespace Myrmex.Modules.Wms.Inventory.Features.InventoryLedger;

internal static class ListInventoryLedgerEntries
{
    internal sealed record Query : ListQuery, IQuery<ServiceResult<ListResult<InventoryLedgerEntryDetails>>>
    {
        public Guid? StockKeepingUnitId { get; init; }
        public Guid? WarehouseId { get; init; }
        public Guid? StorageLocationId { get; init; }
        public string? TransactionType { get; init; }
        public DateTimeOffset? OccurredFromUtc { get; init; }
        public DateTimeOffset? OccurredToUtc { get; init; }
    }

    internal sealed class Handler(WmsDbContext dbContext)
        : IQueryHandler<Query, ServiceResult<ListResult<InventoryLedgerEntryDetails>>>
    {
        public async Task<ServiceResult<ListResult<InventoryLedgerEntryDetails>>> HandleAsync(
            Query query,
            CancellationToken cancellationToken = default)
        {
            DomainValidationFailure[] validationFailures = ValidateQuery(query);

            if (validationFailures.Length > 0)
            {
                return ServiceResult<ListResult<InventoryLedgerEntryDetails>>.Invalid(validationFailures);
            }

            int skip = ListQuery.NormalizeSkip(query.Skip);
            int take = ListQuery.NormalizeTake(query.Take);

            IQueryable<InventoryLedgerEntry> ledgerEntries = dbContext.InventoryLedgerEntries
                .AsNoTracking()
                .ApplyFilters(query);

            int totalCount = await ledgerEntries
                .CountAsync(cancellationToken);

            List<InventoryLedgerEntryDetailsData> itemData = await ledgerEntries
                .ApplySorting(query.SortBy, query.SortDescending)
                .Skip(skip)
                .Take(take)
                .ProjectDetailsData()
                .ToListAsync(cancellationToken);

            List<InventoryLedgerEntryDetails> items = itemData
                .Select(x => x.ToDetails())
                .ToList();

            return ServiceResult<ListResult<InventoryLedgerEntryDetails>>
                .Success(new ListResult<InventoryLedgerEntryDetails>(items, totalCount, skip, take));
        }

        private static DomainValidationFailure[] ValidateQuery(Query query)
        {
            List<DomainValidationFailure> failures = [];

            if (!string.IsNullOrWhiteSpace(query.TransactionType) &&
                !string.Equals(
                    query.TransactionType,
                    nameof(InventoryTransactionType.Adjustment),
                    StringComparison.Ordinal))
            {
                failures.Add(DomainValidationFailure.Unsupported<Query>(nameof(Query.TransactionType)));
            }

            if (query.OccurredFromUtc.HasValue &&
                query.OccurredToUtc.HasValue &&
                query.OccurredFromUtc.Value > query.OccurredToUtc.Value)
            {
                failures.Add(DomainValidationFailure.IncorrectState<Query>(nameof(Query.OccurredFromUtc)));
            }

            return [.. failures];
        }
    }
}
