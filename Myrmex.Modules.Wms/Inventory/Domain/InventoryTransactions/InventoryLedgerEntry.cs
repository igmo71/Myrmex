using Myrmex.Core.Domain;
using Myrmex.Core.Domain.Validation;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;

namespace Myrmex.Modules.Wms.Inventory.Domain.InventoryTransactions;

internal sealed class InventoryLedgerEntry : EntityBase
{
    private InventoryLedgerEntry() { }

    private InventoryLedgerEntry(
        Guid stockKeepingUnitId,
        Guid storageLocationId,
        decimal quantityDelta,
        decimal balanceBefore,
        decimal balanceAfter)
    {
        StockKeepingUnitId = stockKeepingUnitId;
        StorageLocationId = storageLocationId;
        QuantityDelta = quantityDelta;
        BalanceBefore = balanceBefore;
        BalanceAfter = balanceAfter;
    }

    public Guid InventoryTransactionId { get; private set; }
    public InventoryTransaction InventoryTransaction { get; private set; } = null!;

    public Guid StockKeepingUnitId { get; private set; }
    public StockKeepingUnit StockKeepingUnit { get; private set; } = null!;

    public Guid StorageLocationId { get; private set; }
    public StorageLocation StorageLocation { get; private set; } = null!;

    public decimal QuantityDelta { get; private set; }
    public decimal BalanceBefore { get; private set; }
    public decimal BalanceAfter { get; private set; }

    internal static DomainValidationResult Create(
        Guid? stockKeepingUnitId,
        Guid? storageLocationId,
        decimal quantityDelta,
        decimal balanceBefore,
        decimal balanceAfter,
        out InventoryLedgerEntry? entry)
    {
        List<DomainValidationFailure> errors = [];

        if (!stockKeepingUnitId.HasValue || stockKeepingUnitId.Value == Guid.Empty)
        {
            errors.Add(DomainValidationFailure.Required<InventoryLedgerEntry>(nameof(StockKeepingUnitId)));
        }

        if (!storageLocationId.HasValue || storageLocationId.Value == Guid.Empty)
        {
            errors.Add(DomainValidationFailure.Required<InventoryLedgerEntry>(nameof(StorageLocationId)));
        }

        if (quantityDelta == 0)
        {
            errors.Add(DomainValidationFailure.IncorrectState<InventoryLedgerEntry>(nameof(QuantityDelta)));
        }

        if (balanceBefore < 0)
        {
            errors.Add(DomainValidationFailure.MustBeNonNegative<InventoryLedgerEntry>(nameof(BalanceBefore)));
        }

        if (balanceAfter < 0)
        {
            errors.Add(DomainValidationFailure.MustBeNonNegative<InventoryLedgerEntry>(nameof(BalanceAfter)));
        }

        if (balanceAfter != balanceBefore + quantityDelta)
        {
            errors.Add(DomainValidationFailure.IncorrectState<InventoryLedgerEntry>(nameof(BalanceAfter)));
        }

        DomainValidationResult validationResult = DomainValidationResult.From(errors);

        if (!validationResult.IsValid)
        {
            entry = null;
            return validationResult;
        }

        entry = new InventoryLedgerEntry(
            stockKeepingUnitId!.Value,
            storageLocationId!.Value,
            quantityDelta,
            balanceBefore,
            balanceAfter);

        return DomainValidationResult.Valid;
    }
}
