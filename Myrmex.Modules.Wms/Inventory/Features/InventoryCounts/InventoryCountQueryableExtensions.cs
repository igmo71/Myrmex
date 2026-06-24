using Myrmex.Modules.Wms.Inventory.Domain.InventoryCounts;
using Myrmex.Shared.Wms.Inventory;

namespace Myrmex.Modules.Wms.Inventory.Features.InventoryCounts;

internal static class InventoryCountQueryableExtensions
{
    public static IQueryable<InventoryCountDetailsData> ProjectDetailsData(
        this IQueryable<InventoryCount> queryable)
    {
        return queryable.Select(count => new InventoryCountDetailsData(
            count.Id,
            count.RowVersion,
            count.Status.ToString(),
            count.Reason,
            count.CreatedAtUtc,
            count.UpdatedAtUtc,
            count.CompletedAtUtc,
            count.CancelledAtUtc,
            count.CreatedByActorId,
            count.CompletedByActorId,
            count.CancelledByActorId,
            new InventoryCountDetailsData.WarehouseInfo(
                count.WarehouseId,
                count.Warehouse.Code,
                count.Warehouse.Name),
            count.Lines
                .OrderBy(x => x.CreatedAtUtc)
                .ThenBy(x => x.Id)
                .Select(line => new InventoryCountDetailsData.LineInfo(
                    line.Id,
                    line.RowVersion,
                    line.Status.ToString(),
                    line.IsCurrent,
                    line.SystemQuantity,
                    line.CountedQuantity,
                    line.VarianceQuantity,
                    line.ExpectedBalanceVersion,
                    line.Comment,
                    line.CountedByActorId,
                    line.CountedAtUtc,
                    line.AppliedByActorId,
                    line.AppliedAtUtc,
                    line.AppliedInventoryTransactionId,
                    line.SupersedesInventoryCountLineId,
                    line.ReplacementInventoryCountLine == null
                        ? null
                        : line.ReplacementInventoryCountLine.Id,
                    line.CreatedAtUtc,
                    line.UpdatedAtUtc,
                    new InventoryCountDetailsData.StockKeepingUnitInfo(
                        line.StockKeepingUnitId,
                        line.StockKeepingUnit.Code,
                        line.StockKeepingUnit.Name,
                        new InventoryCountDetailsData.UnitOfMeasureInfo(
                            line.StockKeepingUnit.BaseUnitOfMeasureId,
                            line.StockKeepingUnit.BaseUnitOfMeasure.Code,
                            line.StockKeepingUnit.BaseUnitOfMeasure.Symbol)),
                    new InventoryCountDetailsData.StorageLocationInfo(
                        line.StorageLocationId,
                        line.StorageLocation.Code,
                        line.StorageLocation.Name)))
                .ToList()));
    }

    public static InventoryCountDetails ToDetails(this InventoryCountDetailsData data)
    {
        return new InventoryCountDetails(
            data.Id,
            Convert.ToBase64String(data.RowVersion),
            data.Status,
            data.Reason,
            data.CreatedAtUtc,
            data.UpdatedAtUtc,
            data.CompletedAtUtc,
            data.CancelledAtUtc,
            data.CreatedByActorId,
            data.CompletedByActorId,
            data.CancelledByActorId,
            new InventoryCountDetails.WarehouseInfo(
                data.Warehouse.Id,
                data.Warehouse.Code,
                data.Warehouse.Name),
            data.Lines.Select(line => new InventoryCountLineDetails(
                    line.Id,
                    Convert.ToBase64String(line.RowVersion),
                    line.Status,
                    line.IsCurrent,
                    line.SystemQuantity,
                    line.CountedQuantity,
                    line.VarianceQuantity,
                    line.ExpectedBalanceVersion is null
                        ? null
                        : Convert.ToBase64String(line.ExpectedBalanceVersion),
                    line.Comment,
                    line.CountedByActorId,
                    line.CountedAtUtc,
                    line.AppliedByActorId,
                    line.AppliedAtUtc,
                    line.AppliedInventoryTransactionId,
                    line.SupersedesInventoryCountLineId,
                    line.ReplacementInventoryCountLineId,
                    line.CreatedAtUtc,
                    line.UpdatedAtUtc,
                    new InventoryCountLineDetails.StockKeepingUnitInfo(
                        line.Sku.Id,
                        line.Sku.Code,
                        line.Sku.Name,
                        new InventoryCountLineDetails.UnitOfMeasureInfo(
                            line.Sku.BaseUom.Id,
                            line.Sku.BaseUom.Code,
                            line.Sku.BaseUom.Symbol)),
                    new InventoryCountLineDetails.StorageLocationInfo(
                        line.StorageLocation.Id,
                        line.StorageLocation.Code,
                        line.StorageLocation.Name)))
                .ToList());
    }
}

internal sealed record InventoryCountDetailsData(
    Guid Id,
    byte[] RowVersion,
    string Status,
    string? Reason,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    string CreatedByActorId,
    string? CompletedByActorId,
    string? CancelledByActorId,
    InventoryCountDetailsData.WarehouseInfo Warehouse,
    IReadOnlyList<InventoryCountDetailsData.LineInfo> Lines)
{
    public sealed record WarehouseInfo(Guid Id, string Code, string Name);
    public sealed record UnitOfMeasureInfo(Guid Id, string Code, string? Symbol);
    public sealed record StockKeepingUnitInfo(
        Guid Id,
        string Code,
        string Name,
        UnitOfMeasureInfo BaseUom);
    public sealed record StorageLocationInfo(Guid Id, string Code, string Name);
    public sealed record LineInfo(
        Guid Id,
        byte[] RowVersion,
        string Status,
        bool IsCurrent,
        decimal SystemQuantity,
        decimal? CountedQuantity,
        decimal? VarianceQuantity,
        byte[]? ExpectedBalanceVersion,
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
        StockKeepingUnitInfo Sku,
        StorageLocationInfo StorageLocation);
}
