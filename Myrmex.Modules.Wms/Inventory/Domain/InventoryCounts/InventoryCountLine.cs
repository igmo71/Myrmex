using Myrmex.Core.Domain;
using Myrmex.Core.Domain.Validation;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryTransactions;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;

namespace Myrmex.Modules.Wms.Inventory.Domain.InventoryCounts;

internal sealed class InventoryCountLine : EntityBase
{
    public const int CommentMaxLength = 500;
    public const int ActorIdMaxLength = 256;

    private InventoryCountLine()
    {
    }

    private InventoryCountLine(
        Guid stockKeepingUnitId,
        Guid storageLocationId,
        decimal systemQuantity,
        byte[]? expectedBalanceVersion)
    {
        StockKeepingUnitId = stockKeepingUnitId;
        StorageLocationId = storageLocationId;
        SystemQuantity = systemQuantity;
        ExpectedBalanceVersion = expectedBalanceVersion is null ? null : [.. expectedBalanceVersion];
        Status = InventoryCountLineStatus.Pending;
        IsCurrent = true;
    }

    public Guid InventoryCountId { get; private set; }
    public InventoryCount InventoryCount { get; private set; } = null!;

    public Guid StockKeepingUnitId { get; private set; }
    public StockKeepingUnit StockKeepingUnit { get; private set; } = null!;

    public Guid StorageLocationId { get; private set; }
    public StorageLocation StorageLocation { get; private set; } = null!;

    public decimal SystemQuantity { get; private set; }
    public byte[]? ExpectedBalanceVersion { get; private set; }
    public decimal? CountedQuantity { get; private set; }
    public decimal? VarianceQuantity { get; private set; }
    public InventoryCountLineStatus Status { get; private set; }
    public bool IsCurrent { get; private set; }
    public string? Comment { get; private set; }
    public string? CountedByActorId { get; private set; }
    public DateTimeOffset? CountedAtUtc { get; private set; }
    public string? AppliedByActorId { get; private set; }
    public DateTimeOffset? AppliedAtUtc { get; private set; }

    public Guid? AppliedInventoryTransactionId { get; private set; }
    public InventoryTransaction? AppliedInventoryTransaction { get; private set; }

    public Guid? SupersedesInventoryCountLineId { get; private set; }
    public InventoryCountLine? SupersedesInventoryCountLine { get; private set; }
    public InventoryCountLine? ReplacementInventoryCountLine { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    internal static DomainValidationResult Create(
        Guid? stockKeepingUnitId,
        Guid? storageLocationId,
        decimal systemQuantity,
        byte[]? expectedBalanceVersion,
        out InventoryCountLine? line)
    {
        List<DomainValidationFailure> errors = [];

        if (!stockKeepingUnitId.HasValue || stockKeepingUnitId.Value == Guid.Empty)
        {
            errors.Add(DomainValidationFailure.Required<InventoryCountLine>(nameof(StockKeepingUnitId)));
        }

        if (!storageLocationId.HasValue || storageLocationId.Value == Guid.Empty)
        {
            errors.Add(DomainValidationFailure.Required<InventoryCountLine>(nameof(StorageLocationId)));
        }

        if (systemQuantity < 0)
        {
            errors.Add(DomainValidationFailure.MustBeNonNegative<InventoryCountLine>(nameof(SystemQuantity)));
        }

        DomainValidationResult result = DomainValidationResult.From(errors);

        if (!result.IsValid)
        {
            line = null;
            return result;
        }

        line = new InventoryCountLine(
            stockKeepingUnitId!.Value,
            storageLocationId!.Value,
            systemQuantity,
            expectedBalanceVersion);

        return DomainValidationResult.Valid;
    }

    internal DomainValidationResult ValidatePendingRemoval()
    {
        return Status == InventoryCountLineStatus.Pending
            ? DomainValidationResult.Valid
            : DomainValidationResult.Invalid(
                DomainValidationFailure.IncorrectState<InventoryCountLine>(nameof(Status)));
    }
}
