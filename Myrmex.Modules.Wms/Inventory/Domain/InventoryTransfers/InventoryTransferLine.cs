using Myrmex.Core.Domain;
using Myrmex.Core.Domain.Validation;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;

namespace Myrmex.Modules.Wms.Inventory.Domain.InventoryTransfers;

internal sealed class InventoryTransferLine : EntityBase
{
    private InventoryTransferLine()
    {
    }

    private InventoryTransferLine(
        Guid stockKeepingUnitId,
        Guid sourceStorageLocationId,
        Guid destinationStorageLocationId,
        decimal requestedQuantity)
    {
        StockKeepingUnitId = stockKeepingUnitId;
        SourceStorageLocationId = sourceStorageLocationId;
        DestinationStorageLocationId = destinationStorageLocationId;
        RequestedQuantity = requestedQuantity;
    }

    public Guid InventoryTransferId { get; private set; }
    public InventoryTransfer InventoryTransfer { get; private set; } = null!;

    public Guid StockKeepingUnitId { get; private set; }
    public StockKeepingUnit StockKeepingUnit { get; private set; } = null!;

    public Guid SourceStorageLocationId { get; private set; }
    public StorageLocation SourceStorageLocation { get; private set; } = null!;

    public Guid DestinationStorageLocationId { get; private set; }
    public StorageLocation DestinationStorageLocation { get; private set; } = null!;

    public decimal RequestedQuantity { get; private set; }

    public decimal GetMovedQuantity(IEnumerable<InventoryTransferMovement> movements) =>
        movements
            .Where(x => x.InventoryTransferLineId == Id)
            .Where(x => x.FromStorageLocationId == SourceStorageLocationId &&
                        x.ToStorageLocationId == DestinationStorageLocationId)
            .Sum(x => x.Quantity);

    public decimal GetPickedQuantity(IEnumerable<InventoryTransferMovement> movements) =>
        movements
            .Where(x => x.InventoryTransferLineId == Id)
            .Where(x => x.FromStorageLocationId == SourceStorageLocationId)
            .Sum(x => x.Quantity);

    public decimal GetPlacedQuantity(IEnumerable<InventoryTransferMovement> movements) =>
        movements
            .Where(x => x.InventoryTransferLineId == Id)
            .Where(x => x.ToStorageLocationId == DestinationStorageLocationId)
            .Sum(x => x.Quantity);

    public decimal GetInTransitQuantity(IEnumerable<InventoryTransferMovement> movements) =>
        GetPickedQuantity(movements) - GetPlacedQuantity(movements);

    internal static DomainValidationResult Create(
        Guid? stockKeepingUnitId,
        Guid? sourceStorageLocationId,
        Guid? destinationStorageLocationId,
        decimal requestedQuantity,
        out InventoryTransferLine? line)
    {
        List<DomainValidationFailure> errors = [];

        if (!stockKeepingUnitId.HasValue || stockKeepingUnitId.Value == Guid.Empty)
        {
            errors.Add(DomainValidationFailure.Required<InventoryTransferLine>(nameof(StockKeepingUnitId)));
        }

        if (!sourceStorageLocationId.HasValue || sourceStorageLocationId.Value == Guid.Empty)
        {
            errors.Add(DomainValidationFailure.Required<InventoryTransferLine>(nameof(SourceStorageLocationId)));
        }

        if (!destinationStorageLocationId.HasValue || destinationStorageLocationId.Value == Guid.Empty)
        {
            errors.Add(DomainValidationFailure.Required<InventoryTransferLine>(nameof(DestinationStorageLocationId)));
        }

        if (sourceStorageLocationId.HasValue &&
            destinationStorageLocationId.HasValue &&
            sourceStorageLocationId.Value == destinationStorageLocationId.Value)
        {
            errors.Add(DomainValidationFailure.IncorrectState<InventoryTransferLine>(nameof(DestinationStorageLocationId)));
        }

        if (requestedQuantity <= 0)
        {
            errors.Add(DomainValidationFailure.IncorrectState<InventoryTransferLine>(nameof(RequestedQuantity)));
        }

        DomainValidationResult validationResult = DomainValidationResult.From(errors);

        if (!validationResult.IsValid)
        {
            line = null;
            return validationResult;
        }

        line = new InventoryTransferLine(
            stockKeepingUnitId!.Value,
            sourceStorageLocationId!.Value,
            destinationStorageLocationId!.Value,
            requestedQuantity);

        return DomainValidationResult.Valid;
    }
}
