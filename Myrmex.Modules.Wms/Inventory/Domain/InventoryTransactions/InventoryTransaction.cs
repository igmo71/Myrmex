using Myrmex.Core.Domain;
using Myrmex.Core.Domain.Validation;

namespace Myrmex.Modules.Wms.Inventory.Domain.InventoryTransactions;

internal sealed class InventoryTransaction : AggregateRoot
{
    public const int ReasonMaxLength = 500;

    private readonly List<InventoryLedgerEntry> _entries = [];

    private InventoryTransaction() { }

    private InventoryTransaction(
        InventoryTransactionType transactionType,
        string reason,
        DateTimeOffset occurredAtUtc)
    {
        TransactionType = transactionType;
        Reason = reason;
        OccurredAtUtc = occurredAtUtc;
    }

    public InventoryTransactionType TransactionType { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public IReadOnlyCollection<InventoryLedgerEntry> Entries => _entries.AsReadOnly();

    public static DomainValidationResult CreateAdjustment(
        Guid? stockKeepingUnitId,
        Guid? storageLocationId,
        decimal balanceBefore,
        decimal balanceAfter,
        string? reason,
        DateTimeOffset occurredAtUtc,
        out InventoryTransaction? transaction)
    {
        List<DomainValidationFailure> errors = [];
        string trimmedReason = reason?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(trimmedReason))
        {
            errors.Add(DomainValidationFailure.Required<InventoryTransaction>(nameof(Reason)));
        }
        else if (trimmedReason.Length > ReasonMaxLength)
        {
            errors.Add(DomainValidationFailure.TooLong<InventoryTransaction>(nameof(Reason), ReasonMaxLength));
        }

        decimal quantityDelta = balanceAfter - balanceBefore;

        DomainValidationResult entryValidationResult = InventoryLedgerEntry.Create(
            stockKeepingUnitId,
            storageLocationId,
            quantityDelta,
            balanceBefore,
            balanceAfter,
            out InventoryLedgerEntry? entry);

        errors.AddRange(entryValidationResult.Errors);

        DomainValidationResult validationResult = DomainValidationResult.From(errors);

        if (!validationResult.IsValid)
        {
            transaction = null;
            return validationResult;
        }

        transaction = new InventoryTransaction(
            InventoryTransactionType.Adjustment,
            trimmedReason,
            occurredAtUtc);

        transaction._entries.Add(entry!);

        return DomainValidationResult.Valid;
    }
}
