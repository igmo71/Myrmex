using Myrmex.Core.Domain;
using Myrmex.Core.Domain.Validation;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;

namespace Myrmex.Modules.Wms.Inventory.Domain.InventoryCounts;

internal sealed class InventoryCount : AggregateRoot
{
    public const int ReasonMaxLength = 500;
    public const int ActorIdMaxLength = 256;

    private readonly List<InventoryCountLine> _lines = [];

    private InventoryCount()
    {
    }

    private InventoryCount(
        Guid warehouseId,
        string? reason,
        string createdByActorId)
    {
        WarehouseId = warehouseId;
        Reason = reason;
        CreatedByActorId = createdByActorId;
        Status = InventoryCountStatus.Draft;
    }

    public Guid WarehouseId { get; private set; }
    public Warehouse Warehouse { get; private set; } = null!;
    public InventoryCountStatus Status { get; private set; }
    public string? Reason { get; private set; }
    public string CreatedByActorId { get; private set; } = string.Empty;
    public string? CompletedByActorId { get; private set; }
    public string? CancelledByActorId { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyCollection<InventoryCountLine> Lines => _lines.AsReadOnly();

    public static DomainValidationResult Create(
        Guid? warehouseId,
        string? reason,
        string? createdByActorId,
        out InventoryCount? count)
    {
        List<DomainValidationFailure> errors = [];
        string? normalizedReason = NormalizeOptional(reason);
        string normalizedActorId = createdByActorId?.Trim() ?? string.Empty;

        if (!warehouseId.HasValue || warehouseId.Value == Guid.Empty)
        {
            errors.Add(DomainValidationFailure.Required<InventoryCount>(nameof(WarehouseId)));
        }

        if (normalizedReason is not null && normalizedReason.Length > ReasonMaxLength)
        {
            errors.Add(DomainValidationFailure.TooLong<InventoryCount>(nameof(Reason), ReasonMaxLength));
        }

        if (string.IsNullOrWhiteSpace(normalizedActorId))
        {
            errors.Add(DomainValidationFailure.Required<InventoryCount>(nameof(CreatedByActorId)));
        }
        else if (normalizedActorId.Length > ActorIdMaxLength)
        {
            errors.Add(DomainValidationFailure.TooLong<InventoryCount>(nameof(CreatedByActorId), ActorIdMaxLength));
        }

        DomainValidationResult result = DomainValidationResult.From(errors);

        if (!result.IsValid)
        {
            count = null;
            return result;
        }

        count = new InventoryCount(
            warehouseId!.Value,
            normalizedReason,
            normalizedActorId);

        return DomainValidationResult.Valid;
    }

    public DomainValidationResult AddLine(
        Guid? stockKeepingUnitId,
        Guid? storageLocationId,
        decimal systemQuantity,
        byte[]? expectedBalanceVersion,
        out InventoryCountLine? line)
    {
        line = null;

        if (Status is InventoryCountStatus.Completed or InventoryCountStatus.Cancelled)
        {
            return DomainValidationResult.Invalid(
                DomainValidationFailure.IncorrectState<InventoryCount>(nameof(Status)));
        }

        if (stockKeepingUnitId.HasValue &&
            storageLocationId.HasValue &&
            _lines.Any(x =>
                x.IsCurrent &&
                x.StockKeepingUnitId == stockKeepingUnitId.Value &&
                x.StorageLocationId == storageLocationId.Value))
        {
            return DomainValidationResult.Invalid(
                DomainValidationFailure.IncorrectState<InventoryCountLine>(nameof(Lines)));
        }

        DomainValidationResult lineResult = InventoryCountLine.Create(
            stockKeepingUnitId,
            storageLocationId,
            systemQuantity,
            expectedBalanceVersion,
            out line);

        if (!lineResult.IsValid)
        {
            return lineResult;
        }

        var createdLine = line
            ?? throw new InvalidOperationException("InventoryCountLine.Create returned a valid result without a line.");

        _lines.Add(createdLine);
        Touch();
        return DomainValidationResult.Valid;
    }

    public DomainValidationResult RemovePendingLine(Guid lineId, out InventoryCountLine? removedLine)
    {
        removedLine = _lines.SingleOrDefault(x => x.Id == lineId);

        if (removedLine is null)
        {
            return DomainValidationResult.Invalid(
                DomainValidationFailure.Required<InventoryCountLine>(nameof(lineId)));
        }

        DomainValidationResult lineResult = removedLine.ValidatePendingRemoval();

        if (!lineResult.IsValid)
        {
            return lineResult;
        }

        _lines.Remove(removedLine);
        Touch();
        return DomainValidationResult.Valid;
    }

    public DomainValidationResult RecordLineCount(
        Guid lineId,
        decimal countedQuantity,
        string? comment,
        string? actorId,
        DateTimeOffset countedAtUtc)
    {
        if (Status is InventoryCountStatus.Completed or InventoryCountStatus.Cancelled)
        {
            return DomainValidationResult.Invalid(
                DomainValidationFailure.IncorrectState<InventoryCount>(nameof(Status)));
        }

        InventoryCountLine? line = _lines.SingleOrDefault(x => x.Id == lineId);

        if (line is null)
        {
            return DomainValidationResult.Invalid(
                DomainValidationFailure.Required<InventoryCountLine>(nameof(lineId)));
        }

        DomainValidationResult lineResult = line.RecordCount(
            countedQuantity,
            comment,
            actorId,
            countedAtUtc);

        if (!lineResult.IsValid)
        {
            return lineResult;
        }

        if (Status == InventoryCountStatus.Draft)
        {
            Status = InventoryCountStatus.InProgress;
        }

        Touch();
        return DomainValidationResult.Valid;
    }

    private static string? NormalizeOptional(string? value)
    {
        string? normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
