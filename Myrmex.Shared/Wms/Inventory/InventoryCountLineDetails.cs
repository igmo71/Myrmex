namespace Myrmex.Shared.Wms.Inventory;

public sealed record InventoryCountLineDetails(
    Guid Id,
    string LineVersion,
    string Status,
    bool IsCurrent,
    decimal SystemQuantity,
    decimal? CountedQuantity,
    decimal? VarianceQuantity,
    string? ExpectedBalanceVersion,
    string? Comment,
    string? CountedByActorId,
    DateTimeOffset? CountedAtUtc,
    string? AppliedByActorId,
    DateTimeOffset? AppliedAtUtc,
    Guid? AppliedInventoryTransactionId,
    Guid? SupersedesInventoryCountLineId,
    Guid? ReplacementInventoryCountLineId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    InventoryCountLineDetails.StockKeepingUnitInfo Sku,
    InventoryCountLineDetails.StorageLocationInfo StorageLocation)
{
    public sealed record StockKeepingUnitInfo(
        Guid Id,
        string Code,
        string Name,
        UnitOfMeasureInfo BaseUom);

    public sealed record UnitOfMeasureInfo(Guid Id, string Code, string? Symbol);

    public sealed record StorageLocationInfo(Guid Id, string Code, string Name);
}
