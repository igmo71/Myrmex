using Myrmex.Core.Domain;
using Myrmex.Core.Domain.Validation;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryTransactions;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;

namespace Myrmex.Modules.Wms.Inventory.Domain.InventoryTransfers;

internal sealed class InventoryTransferMovement : EntityBase
{
    private InventoryTransferMovement()
    {
    }

    private InventoryTransferMovement(
        Guid inventoryTransferLineId,
        Guid inventoryTransactionId,
        Guid fromStorageLocationId,
        Guid toStorageLocationId,
        decimal quantity,
        DateTimeOffset occurredAtUtc)
    {
        InventoryTransferLineId = inventoryTransferLineId;
        InventoryTransactionId = inventoryTransactionId;
        FromStorageLocationId = fromStorageLocationId;
        ToStorageLocationId = toStorageLocationId;
        Quantity = quantity;
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid InventoryTransferId { get; private set; }
    public InventoryTransfer InventoryTransfer { get; private set; } = null!;

    public Guid InventoryTransferLineId { get; private set; }
    public InventoryTransferLine InventoryTransferLine { get; private set; } = null!;

    public Guid InventoryTransactionId { get; private set; }
    public InventoryTransaction InventoryTransaction { get; private set; } = null!;

    public Guid FromStorageLocationId { get; private set; }
    public StorageLocation FromStorageLocation { get; private set; } = null!;

    public Guid ToStorageLocationId { get; private set; }
    public StorageLocation ToStorageLocation { get; private set; } = null!;

    public decimal Quantity { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    internal static DomainValidationResult Create(
        Guid? inventoryTransferLineId,
        Guid? inventoryTransactionId,
        Guid? fromStorageLocationId,
        Guid? toStorageLocationId,
        decimal quantity,
        DateTimeOffset occurredAtUtc,
        out InventoryTransferMovement? movement)
    {
        List<DomainValidationFailure> errors = [];

        if (!inventoryTransferLineId.HasValue || inventoryTransferLineId.Value == Guid.Empty)
        {
            errors.Add(DomainValidationFailure.Required<InventoryTransferMovement>(nameof(InventoryTransferLineId)));
        }

        if (!inventoryTransactionId.HasValue || inventoryTransactionId.Value == Guid.Empty)
        {
            errors.Add(DomainValidationFailure.Required<InventoryTransferMovement>(nameof(InventoryTransactionId)));
        }

        if (!fromStorageLocationId.HasValue || fromStorageLocationId.Value == Guid.Empty)
        {
            errors.Add(DomainValidationFailure.Required<InventoryTransferMovement>(nameof(FromStorageLocationId)));
        }

        if (!toStorageLocationId.HasValue || toStorageLocationId.Value == Guid.Empty)
        {
            errors.Add(DomainValidationFailure.Required<InventoryTransferMovement>(nameof(ToStorageLocationId)));
        }

        if (fromStorageLocationId.HasValue &&
            toStorageLocationId.HasValue &&
            fromStorageLocationId.Value == toStorageLocationId.Value)
        {
            errors.Add(DomainValidationFailure.IncorrectState<InventoryTransferMovement>(nameof(ToStorageLocationId)));
        }

        if (quantity <= 0)
        {
            errors.Add(DomainValidationFailure.IncorrectState<InventoryTransferMovement>(nameof(Quantity)));
        }

        DomainValidationResult validationResult = DomainValidationResult.From(errors);

        if (!validationResult.IsValid)
        {
            movement = null;
            return validationResult;
        }

        movement = new InventoryTransferMovement(
            inventoryTransferLineId!.Value,
            inventoryTransactionId!.Value,
            fromStorageLocationId!.Value,
            toStorageLocationId!.Value,
            quantity,
            occurredAtUtc);

        return DomainValidationResult.Valid;
    }
}
