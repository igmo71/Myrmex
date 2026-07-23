using Myrmex.Core.Domain;
using Myrmex.Core.Domain.Validation;
using Myrmex.Modules.Wms.Domain;

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

    internal sealed record ReceivingChange(
        Guid? StockKeepingUnitId,
        decimal QuantityDelta,
        decimal BalanceBefore,
        decimal BalanceAfter);

    public static DomainValidationResult CreateReceiving(
        Guid? receivingLocationId,
        IEnumerable<ReceivingChange>? changes,
        string? reason,
        DateTimeOffset occurredAtUtc,
        out InventoryTransaction? transaction)
    {
        ReceivingChange[] materializedChanges = changes?.ToArray() ?? [];
        List<DomainValidationFailure> errors = [];
        List<InventoryLedgerEntry> entries = [];
        string trimmedReason = reason?.Trim() ?? string.Empty;

        if (!receivingLocationId.HasValue || receivingLocationId.Value == Guid.Empty)
        {
            errors.Add(DomainValidationFailure.Required<InventoryLedgerEntry>(
                nameof(InventoryLedgerEntry.StorageLocationId)));
        }

        if (string.IsNullOrWhiteSpace(trimmedReason))
        {
            errors.Add(DomainValidationFailure.Required<InventoryTransaction>(nameof(Reason)));
        }
        else if (trimmedReason.Length > ReasonMaxLength)
        {
            errors.Add(DomainValidationFailure.TooLong<InventoryTransaction>(nameof(Reason), ReasonMaxLength));
        }

        if (materializedChanges.Length == 0)
        {
            errors.Add(DomainValidationFailure.Required<InventoryTransaction>(nameof(Entries)));
        }

        for (int index = 0; index < materializedChanges.Length; index++)
        {
            ReceivingChange change = materializedChanges[index];
            string prefix = $"Changes[{index}]";

            if (change.QuantityDelta <= 0)
            {
                errors.Add(DomainValidationFailure.IncorrectState<InventoryLedgerEntry>(
                    $"{prefix}.{nameof(change.QuantityDelta)}"));
            }

            AddPersistenceFailure(change.QuantityDelta, $"{prefix}.{nameof(change.QuantityDelta)}", errors);
            AddPersistenceFailure(change.BalanceBefore, $"{prefix}.{nameof(change.BalanceBefore)}", errors);
            AddPersistenceFailure(change.BalanceAfter, $"{prefix}.{nameof(change.BalanceAfter)}", errors);

            DomainValidationResult entryResult = InventoryLedgerEntry.Create(
                change.StockKeepingUnitId,
                receivingLocationId,
                change.QuantityDelta,
                change.BalanceBefore,
                change.BalanceAfter,
                out InventoryLedgerEntry? entry);
            errors.AddRange(entryResult.Errors);
            if (entryResult.IsValid && entry is not null)
            {
                entries.Add(entry);
            }
        }

        DomainValidationResult result = DomainValidationResult.From(errors);
        if (!result.IsValid)
        {
            transaction = null;
            return result;
        }

        transaction = new InventoryTransaction(
            InventoryTransactionType.Receiving,
            trimmedReason,
            occurredAtUtc);
        transaction._entries.AddRange(entries);
        return DomainValidationResult.Valid;
    }

    private static void AddPersistenceFailure(
        decimal value,
        string property,
        ICollection<DomainValidationFailure> errors)
    {
        DomainValidationFailure? failure =
            WmsQuantityPersistence.Validate<InventoryLedgerEntry>(value, property);
        if (failure is not null)
        {
            errors.Add(failure);
        }
    }

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

    public static DomainValidationResult CreateTransfer(
        Guid? stockKeepingUnitId,
        Guid? fromStorageLocationId,
        Guid? toStorageLocationId,
        decimal fromBalanceBefore,
        decimal fromBalanceAfter,
        decimal toBalanceBefore,
        decimal toBalanceAfter,
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

        decimal fromQuantityDelta = fromBalanceAfter - fromBalanceBefore;
        decimal toQuantityDelta = toBalanceAfter - toBalanceBefore;

        DomainValidationResult fromEntryValidationResult = InventoryLedgerEntry.Create(
            stockKeepingUnitId,
            fromStorageLocationId,
            fromQuantityDelta,
            fromBalanceBefore,
            fromBalanceAfter,
            out InventoryLedgerEntry? fromEntry);

        DomainValidationResult toEntryValidationResult = InventoryLedgerEntry.Create(
            stockKeepingUnitId,
            toStorageLocationId,
            toQuantityDelta,
            toBalanceBefore,
            toBalanceAfter,
            out InventoryLedgerEntry? toEntry);

        errors.AddRange(fromEntryValidationResult.Errors);
        errors.AddRange(toEntryValidationResult.Errors);

        if (fromQuantityDelta >= 0)
        {
            errors.Add(DomainValidationFailure.IncorrectState<InventoryLedgerEntry>(nameof(InventoryLedgerEntry.QuantityDelta)));
        }

        if (toQuantityDelta <= 0)
        {
            errors.Add(DomainValidationFailure.IncorrectState<InventoryLedgerEntry>(nameof(InventoryLedgerEntry.QuantityDelta)));
        }

        if (fromQuantityDelta + toQuantityDelta != 0)
        {
            errors.Add(DomainValidationFailure.IncorrectState<InventoryTransaction>(nameof(Entries)));
        }

        DomainValidationResult validationResult = DomainValidationResult.From(errors);

        if (!validationResult.IsValid)
        {
            transaction = null;
            return validationResult;
        }

        transaction = new InventoryTransaction(
            InventoryTransactionType.Transfer,
            trimmedReason,
            occurredAtUtc);

        transaction._entries.Add(fromEntry!);
        transaction._entries.Add(toEntry!);

        return DomainValidationResult.Valid;
    }
}
