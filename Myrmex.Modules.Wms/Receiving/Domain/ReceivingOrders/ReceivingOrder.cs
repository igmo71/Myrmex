using Myrmex.Core.Domain;
using Myrmex.Core.Domain.Validation;
using Myrmex.Modules.Wms.Domain;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryTransactions;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;

namespace Myrmex.Modules.Wms.Receiving.Domain.ReceivingOrders;

internal sealed class ReceivingOrder : AggregateRoot
{
    public const int NumberMaxLength = DomainTextLengths.Code;

    private readonly List<ReceivingOrderLine> _lines = [];

    private ReceivingOrder()
    {
    }

    private ReceivingOrder(
        string number,
        Guid warehouseId,
        Guid receivingLocationId)
    {
        Number = number;
        WarehouseId = warehouseId;
        ReceivingLocationId = receivingLocationId;
        Status = ReceivingOrderStatus.Draft;
    }

    public string Number { get; private set; } = string.Empty;

    public Guid WarehouseId { get; private set; }
    public Warehouse Warehouse { get; private set; } = null!;

    public Guid ReceivingLocationId { get; private set; }
    public StorageLocation ReceivingLocation { get; private set; } = null!;

    public ReceivingOrderStatus Status { get; private set; }

    public DateTimeOffset? StartedAtUtc { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public Guid? InventoryTransactionId { get; private set; }
    public InventoryTransaction? InventoryTransaction { get; private set; }

    internal ExternalImportState? ImportState { get; private set; }

    public Guid? ExternalRefKey => ImportState?.RefKey;

    public byte[]? ExternalDataVersion => ImportState?.DataVersion;

    public DateTimeOffset? LastImportedAtUtc => ImportState?.ImportedAtUtc;

    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<ReceivingOrderLine> Lines => _lines.AsReadOnly();

    public bool IsFullyReceived =>
        _lines.Count > 0 && _lines.All(line => line.IsFullyReceived);

    public bool HasCompletePersistedCompletedInvariant =>
        HasValidCorePersistenceInvariant &&
        Status == ReceivingOrderStatus.Completed &&
        StartedAtUtc.HasValue &&
        CompletedAtUtc.HasValue &&
        InventoryTransactionId.HasValue &&
        InventoryTransactionId.Value != Guid.Empty &&
        _lines.All(line => line.ReceivedQuantity == line.PlannedQuantity);

    public bool HasValidDraftPersistenceInvariant =>
        HasValidCorePersistenceInvariant &&
        Status == ReceivingOrderStatus.Draft &&
        !StartedAtUtc.HasValue &&
        !CompletedAtUtc.HasValue &&
        !InventoryTransactionId.HasValue &&
        _lines.All(line => line.ReceivedQuantity == 0);

    public void RecordExternalImport(
        Guid externalRefKey,
        byte[] externalDataVersion,
        DateTimeOffset importedAtUtc)
    {
        if (ImportState is null)
        {
            ImportState = ExternalImportState.Create(
                externalRefKey,
                externalDataVersion,
                importedAtUtc);
            return;
        }

        if (ImportState.RefKey != externalRefKey)
        {
            throw new InvalidOperationException("ReceivingOrder external identity cannot change.");
        }

        ImportState.RecordImport(externalDataVersion, importedAtUtc);
    }

    private bool HasValidCorePersistenceInvariant =>
        !string.IsNullOrWhiteSpace(Number) &&
        Number.Length <= NumberMaxLength &&
        string.Equals(Number, DomainText.NormalizeCode(Number), StringComparison.Ordinal) &&
        WarehouseId != Guid.Empty &&
        ReceivingLocationId != Guid.Empty &&
        _lines.Count > 0 &&
        _lines.All(line =>
            line.Id != Guid.Empty &&
            line.ReceivingOrderId == Id &&
            line.StockKeepingUnitId != Guid.Empty &&
            line.PlannedQuantity > 0 &&
            WmsQuantityPersistence.IsExactlyRepresentable(line.PlannedQuantity) &&
            line.ReceivedQuantity >= 0 &&
            WmsQuantityPersistence.IsExactlyRepresentable(line.ReceivedQuantity) &&
            line.ReceivedQuantity <= line.PlannedQuantity) &&
        _lines.Select(line => line.StockKeepingUnitId).Distinct().Count() == _lines.Count;

    internal sealed record DraftLine(
        Guid? LineId,
        Guid? StockKeepingUnitId,
        decimal PlannedQuantity);

    internal sealed record ImportedDraftLine(
        Guid? StockKeepingUnitId,
        decimal PlannedQuantity);

    public static DomainValidationResult Create(
        string? number,
        Guid? warehouseId,
        Guid? receivingLocationId,
        IEnumerable<DraftLine>? lines,
        out ReceivingOrder? order)
    {
        DraftLine[] materializedLines = lines?.ToArray() ?? [];
        List<DomainValidationFailure> errors = ValidateHeader(
            number,
            warehouseId,
            receivingLocationId);

        errors.AddRange(ValidateDraftPlan(materializedLines, existingLines: null));

        DomainValidationResult result = DomainValidationResult.From(errors);
        if (!result.IsValid)
        {
            order = null;
            return result;
        }

        order = new ReceivingOrder(
            DomainText.NormalizeCode(number),
            warehouseId!.Value,
            receivingLocationId!.Value);

        foreach (DraftLine draftLine in materializedLines)
        {
            DomainValidationResult lineResult = ReceivingOrderLine.Create(
                order.Id,
                draftLine.StockKeepingUnitId,
                draftLine.PlannedQuantity,
                out ReceivingOrderLine? line);

            if (!lineResult.IsValid || line is null)
            {
                throw new InvalidOperationException(
                    "Validated receiving plan could not create a receiving order line.");
            }

            order._lines.Add(line);
        }

        return DomainValidationResult.Valid;
    }

    public DomainValidationResult ReplaceDraft(
        string? number,
        Guid? warehouseId,
        Guid? receivingLocationId,
        IEnumerable<DraftLine>? lines,
        out IReadOnlyList<ReceivingOrderLine> removedLines)
    {
        removedLines = [];

        if (Status != ReceivingOrderStatus.Draft ||
            !HasValidDraftPersistenceInvariant)
        {
            return DomainValidationResult.Invalid(
                DomainValidationFailure.IncorrectState<ReceivingOrder>(nameof(Status)));
        }

        DraftLine[] materializedLines = lines?.ToArray() ?? [];
        List<DomainValidationFailure> errors = ValidateHeader(
            number,
            warehouseId,
            receivingLocationId);

        errors.AddRange(ValidateDraftPlan(materializedLines, _lines));

        DomainValidationResult result = DomainValidationResult.From(errors);
        if (!result.IsValid)
        {
            return result;
        }

        Dictionary<Guid, ReceivingOrderLine> existingById =
            _lines.ToDictionary(line => line.Id);
        HashSet<Guid> retainedIds = materializedLines
            .Where(line => line.LineId.HasValue)
            .Select(line => line.LineId!.Value)
            .ToHashSet();
        ReceivingOrderLine[] removed =
            [.. _lines.Where(line => !retainedIds.Contains(line.Id))];

        Number = DomainText.NormalizeCode(number);
        WarehouseId = warehouseId!.Value;
        ReceivingLocationId = receivingLocationId!.Value;

        foreach (DraftLine draftLine in materializedLines)
        {
            if (draftLine.LineId is Guid retainedId)
            {
                existingById[retainedId].ReplaceDraftPlan(
                    draftLine.StockKeepingUnitId!.Value,
                    draftLine.PlannedQuantity);
                continue;
            }

            DomainValidationResult lineResult = ReceivingOrderLine.Create(
                Id,
                draftLine.StockKeepingUnitId,
                draftLine.PlannedQuantity,
                out ReceivingOrderLine? line);

            if (!lineResult.IsValid || line is null)
            {
                throw new InvalidOperationException(
                    "Validated receiving plan could not create a receiving order line.");
            }

            _lines.Add(line);
        }

        foreach (ReceivingOrderLine removedLine in removed)
        {
            _lines.Remove(removedLine);
        }

        removedLines = removed;
        Touch();
        return DomainValidationResult.Valid;
    }

    public DomainValidationResult ReconcileImportedDraftPlan(
        string? number,
        Guid? warehouseId,
        Guid? receivingLocationId,
        IEnumerable<ImportedDraftLine>? lines,
        out IReadOnlyList<ReceivingOrderLine> removedLines)
    {
        removedLines = [];

        if (Status != ReceivingOrderStatus.Draft ||
            !HasValidDraftPersistenceInvariant)
        {
            return DomainValidationResult.Invalid(
                DomainValidationFailure.IncorrectState<ReceivingOrder>(nameof(Status)));
        }

        ImportedDraftLine[] materializedLines = lines?.ToArray() ?? [];
        List<DomainValidationFailure> errors = ValidateHeader(
            number,
            warehouseId,
            receivingLocationId);
        errors.AddRange(ValidateDraftPlan(
            materializedLines
                .Select(line => new DraftLine(null, line.StockKeepingUnitId, line.PlannedQuantity))
                .ToArray(),
            existingLines: null));

        DomainValidationResult result = DomainValidationResult.From(errors);
        if (!result.IsValid)
        {
            return result;
        }

        Dictionary<Guid, ReceivingOrderLine> existingBySku = _lines
            .ToDictionary(line => line.StockKeepingUnitId);
        HashSet<Guid> importedSkuIds = materializedLines
            .Select(line => line.StockKeepingUnitId!.Value)
            .ToHashSet();
        ReceivingOrderLine[] removed =
            [.. _lines.Where(line => !importedSkuIds.Contains(line.StockKeepingUnitId))];

        Number = DomainText.NormalizeCode(number);
        WarehouseId = warehouseId!.Value;
        ReceivingLocationId = receivingLocationId!.Value;

        foreach (ImportedDraftLine importedLine in materializedLines)
        {
            Guid stockKeepingUnitId = importedLine.StockKeepingUnitId!.Value;
            if (existingBySku.TryGetValue(stockKeepingUnitId, out ReceivingOrderLine? existingLine))
            {
                existingLine.ReplaceDraftPlan(stockKeepingUnitId, importedLine.PlannedQuantity);
                continue;
            }

            DomainValidationResult lineResult = ReceivingOrderLine.Create(
                Id,
                stockKeepingUnitId,
                importedLine.PlannedQuantity,
                out ReceivingOrderLine? line);

            if (!lineResult.IsValid || line is null)
            {
                throw new InvalidOperationException(
                    "Validated imported receiving plan could not create a receiving order line.");
            }

            _lines.Add(line);
        }

        foreach (ReceivingOrderLine removedLine in removed)
        {
            _lines.Remove(removedLine);
        }

        removedLines = removed;
        Touch();
        return DomainValidationResult.Valid;
    }

    public DomainValidationResult Start(DateTimeOffset startedAtUtc)
    {
        if (Status == ReceivingOrderStatus.InProgress)
        {
            return DomainValidationResult.Valid;
        }

        if (Status != ReceivingOrderStatus.Draft ||
            !HasValidDraftPersistenceInvariant ||
            _lines.Count == 0)
        {
            return DomainValidationResult.Invalid(
                DomainValidationFailure.IncorrectState<ReceivingOrder>(nameof(Status)));
        }

        Status = ReceivingOrderStatus.InProgress;
        StartedAtUtc = startedAtUtc;
        Touch();
        return DomainValidationResult.Valid;
    }

    public DomainValidationResult Receive(
        Guid lineId,
        decimal quantity)
    {
        if (Status != ReceivingOrderStatus.InProgress)
        {
            return DomainValidationResult.Invalid(
                DomainValidationFailure.IncorrectState<ReceivingOrder>(nameof(Status)));
        }

        ReceivingOrderLine? line = _lines.SingleOrDefault(candidate => candidate.Id == lineId);
        if (line is null)
        {
            return DomainValidationResult.Invalid(
                DomainValidationFailure.Required<ReceivingOrderLine>(nameof(lineId)));
        }

        DomainValidationResult result = line.Receive(quantity);
        if (result.IsValid)
        {
            Touch();
        }

        return result;
    }

    public DomainValidationResult Complete(
        Guid? inventoryTransactionId,
        DateTimeOffset completedAtUtc)
    {
        if (Status == ReceivingOrderStatus.Completed)
        {
            return HasCompletePersistedCompletedInvariant
                ? DomainValidationResult.Valid
                : DomainValidationResult.Invalid(
                    DomainValidationFailure.IncorrectState<ReceivingOrder>(nameof(Status)));
        }

        List<DomainValidationFailure> errors = [];

        if (Status != ReceivingOrderStatus.InProgress ||
            !StartedAtUtc.HasValue ||
            CompletedAtUtc.HasValue ||
            InventoryTransactionId.HasValue)
        {
            errors.Add(DomainValidationFailure.IncorrectState<ReceivingOrder>(nameof(Status)));
        }

        if (!IsFullyReceived)
        {
            errors.Add(DomainValidationFailure.IncorrectState<ReceivingOrder>(nameof(Lines)));
        }

        if (!inventoryTransactionId.HasValue || inventoryTransactionId.Value == Guid.Empty)
        {
            errors.Add(DomainValidationFailure.Required<ReceivingOrder>(nameof(InventoryTransactionId)));
        }

        DomainValidationResult result = DomainValidationResult.From(errors);
        if (!result.IsValid)
        {
            return result;
        }

        Status = ReceivingOrderStatus.Completed;
        CompletedAtUtc = completedAtUtc;
        InventoryTransactionId = inventoryTransactionId!.Value;
        Touch();
        return DomainValidationResult.Valid;
    }

    private static List<DomainValidationFailure> ValidateHeader(
        string? number,
        Guid? warehouseId,
        Guid? receivingLocationId)
    {
        List<DomainValidationFailure> errors = [];
        string normalizedNumber = DomainText.NormalizeCode(number);

        if (string.IsNullOrWhiteSpace(normalizedNumber))
        {
            errors.Add(DomainValidationFailure.Required<ReceivingOrder>(nameof(Number)));
        }
        else if (normalizedNumber.Length > NumberMaxLength)
        {
            errors.Add(DomainValidationFailure.TooLong<ReceivingOrder>(nameof(Number), NumberMaxLength));
        }

        if (!warehouseId.HasValue || warehouseId.Value == Guid.Empty)
        {
            errors.Add(DomainValidationFailure.Required<ReceivingOrder>(nameof(WarehouseId)));
        }

        if (!receivingLocationId.HasValue || receivingLocationId.Value == Guid.Empty)
        {
            errors.Add(DomainValidationFailure.Required<ReceivingOrder>(nameof(ReceivingLocationId)));
        }

        return errors;
    }

    private static IEnumerable<DomainValidationFailure> ValidateDraftPlan(
        IReadOnlyList<DraftLine> proposedLines,
        IReadOnlyCollection<ReceivingOrderLine>? existingLines)
    {
        List<DomainValidationFailure> errors = [];

        if (proposedLines.Count == 0)
        {
            errors.Add(DomainValidationFailure.Required<ReceivingOrder>(nameof(Lines)));
            return errors;
        }

        HashSet<Guid> seenLineIds = [];
        HashSet<Guid> seenSkuIds = [];
        HashSet<Guid>? existingLineIds = existingLines?
            .Select(line => line.Id)
            .ToHashSet();

        for (int index = 0; index < proposedLines.Count; index++)
        {
            DraftLine line = proposedLines[index];
            string lineIdProperty = $"Lines[{index}].LineId";
            string skuProperty = $"Lines[{index}].StockKeepingUnitId";
            string quantityProperty = $"Lines[{index}].PlannedQuantity";

            if (line.LineId is Guid lineId)
            {
                if (lineId == Guid.Empty)
                {
                    errors.Add(DomainValidationFailure.Required<ReceivingOrderLine>(lineIdProperty));
                }
                else if (!seenLineIds.Add(lineId))
                {
                    errors.Add(new DomainValidationFailure(
                        "ReceivingOrderLine.DuplicateLineId",
                        "LineId must be unique within the submitted plan.",
                        lineIdProperty));
                }
                else if (existingLineIds is null || !existingLineIds.Contains(lineId))
                {
                    errors.Add(new DomainValidationFailure(
                        "ReceivingOrderLine.ForeignLine",
                        "LineId does not belong to this receiving order.",
                        lineIdProperty));
                }
            }

            DomainValidationResult planResult = ReceivingOrderLine.ValidatePlan(
                line.StockKeepingUnitId,
                line.PlannedQuantity);

            foreach (DomainValidationFailure failure in planResult.Errors)
            {
                errors.Add(failure with
                {
                    Property = failure.Property == nameof(ReceivingOrderLine.StockKeepingUnitId)
                        ? skuProperty
                        : quantityProperty
                });
            }

            if (line.StockKeepingUnitId is Guid skuId &&
                skuId != Guid.Empty &&
                !seenSkuIds.Add(skuId))
            {
                errors.Add(new DomainValidationFailure(
                    "ReceivingOrderLine.DuplicateSku",
                    "Each SKU may appear only once in a receiving order.",
                    skuProperty));
            }
        }

        return errors;
    }
}
