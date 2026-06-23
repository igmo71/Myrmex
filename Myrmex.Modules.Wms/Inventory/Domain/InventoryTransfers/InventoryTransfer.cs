using Myrmex.Core.Domain;
using Myrmex.Core.Domain.Validation;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;

namespace Myrmex.Modules.Wms.Inventory.Domain.InventoryTransfers;

internal sealed class InventoryTransfer : AggregateRoot
{
    public const int CodeMaxLength = DomainTextLengths.Code;

    private readonly List<InventoryTransferLine> _lines = [];
    private readonly List<InventoryTransferMovement> _movements = [];

    private InventoryTransfer()
    {
    }

    private InventoryTransfer(
        string code,
        Guid sourceWarehouseId,
        Guid destinationWarehouseId,
        Guid? transitStorageLocationId)
    {
        Code = code;
        SourceWarehouseId = sourceWarehouseId;
        DestinationWarehouseId = destinationWarehouseId;
        TransitStorageLocationId = transitStorageLocationId;
        Status = InventoryTransferStatus.Created;
    }

    public string Code { get; private set; } = string.Empty;

    public Guid SourceWarehouseId { get; private set; }
    public Warehouse SourceWarehouse { get; private set; } = null!;

    public Guid DestinationWarehouseId { get; private set; }
    public Warehouse DestinationWarehouse { get; private set; } = null!;

    public Guid? TransitStorageLocationId { get; private set; }
    public StorageLocation? TransitStorageLocation { get; private set; }

    public InventoryTransferStatus Status { get; private set; }

    public IReadOnlyCollection<InventoryTransferLine> Lines => _lines.AsReadOnly();

    public IReadOnlyCollection<InventoryTransferMovement> Movements => _movements.AsReadOnly();

    public bool UsesTransit => TransitStorageLocationId.HasValue;

    internal void AddMovement(InventoryTransferMovement movement)
    {
        ArgumentNullException.ThrowIfNull(movement);
        _movements.Add(movement);
        Status = InventoryTransferStatus.InProgress;
        Touch();
    }

    internal void RecalculateStatus()
    {
        if (_lines.Count == 0 || _movements.Count == 0)
        {
            Status = InventoryTransferStatus.Created;
            return;
        }

        bool allPlaced = _lines.All(line =>
            line.GetPlacedQuantity(_movements) >= line.RequestedQuantity &&
            line.GetInTransitQuantity(_movements) == 0);

        Status = allPlaced
            ? InventoryTransferStatus.Completed
            : InventoryTransferStatus.InProgress;
    }

    public static DomainValidationResult Create(
        string? code,
        Guid? sourceWarehouseId,
        Guid? destinationWarehouseId,
        Guid? transitStorageLocationId,
        IEnumerable<InventoryTransferLine>? lines,
        out InventoryTransfer? transfer)
    {
        List<DomainValidationFailure> errors = [];
        string normalizedCode = DomainText.NormalizeCode(code);

        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            errors.Add(DomainValidationFailure.Required<InventoryTransfer>(nameof(Code)));
        }
        else if (normalizedCode.Length > CodeMaxLength)
        {
            errors.Add(DomainValidationFailure.TooLong<InventoryTransfer>(nameof(Code), CodeMaxLength));
        }

        if (!sourceWarehouseId.HasValue || sourceWarehouseId.Value == Guid.Empty)
        {
            errors.Add(DomainValidationFailure.Required<InventoryTransfer>(nameof(SourceWarehouseId)));
        }

        if (!destinationWarehouseId.HasValue || destinationWarehouseId.Value == Guid.Empty)
        {
            errors.Add(DomainValidationFailure.Required<InventoryTransfer>(nameof(DestinationWarehouseId)));
        }

        if (sourceWarehouseId.HasValue &&
            destinationWarehouseId.HasValue &&
            sourceWarehouseId.Value != destinationWarehouseId.Value)
        {
            errors.Add(DomainValidationFailure.Unsupported<InventoryTransfer>(nameof(DestinationWarehouseId)));
        }

        if (transitStorageLocationId.HasValue && transitStorageLocationId.Value == Guid.Empty)
        {
            errors.Add(DomainValidationFailure.Required<InventoryTransfer>(nameof(TransitStorageLocationId)));
        }

        InventoryTransferLine[] materializedLines = lines?.ToArray() ?? [];

        if (materializedLines.Length == 0)
        {
            errors.Add(DomainValidationFailure.Required<InventoryTransfer>(nameof(Lines)));
        }

        DomainValidationResult validationResult = DomainValidationResult.From(errors);

        if (!validationResult.IsValid)
        {
            transfer = null;
            return validationResult;
        }

        transfer = new InventoryTransfer(
            normalizedCode,
            sourceWarehouseId!.Value,
            destinationWarehouseId!.Value,
            transitStorageLocationId);

        transfer._lines.AddRange(materializedLines);

        return DomainValidationResult.Valid;
    }
}
