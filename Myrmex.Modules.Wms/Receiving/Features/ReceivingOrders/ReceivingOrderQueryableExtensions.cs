using Myrmex.Modules.Wms.Receiving.Domain.ReceivingOrders;
using Myrmex.Shared.Wms.Receiving;

namespace Myrmex.Modules.Wms.Receiving.Features.ReceivingOrders;

internal static class ReceivingOrderQueryableExtensions
{
    public static IQueryable<ReceivingOrderDetailsData> ProjectDetailsData(
        this IQueryable<ReceivingOrder> queryable)
    {
        return queryable.Select(order => new ReceivingOrderDetailsData(
            order.Id,
            order.RowVersion,
            order.Number,
            order.Status.ToString(),
            order.CreatedAtUtc,
            order.UpdatedAtUtc,
            order.StartedAtUtc,
            order.CompletedAtUtc,
            order.InventoryTransactionId,
            new(order.WarehouseId, order.Warehouse.Code, order.Warehouse.Name),
            new(order.ReceivingLocationId, order.ReceivingLocation.Code, order.ReceivingLocation.Name),
            order.Lines.Count,
            order.Lines.Sum(x => x.PlannedQuantity),
            order.Lines.Sum(x => x.ReceivedQuantity),
            order.Lines.Sum(x => x.PlannedQuantity - x.ReceivedQuantity),
            order.Lines
                .OrderBy(x => x.CreatedAtUtc)
                .ThenBy(x => x.Id)
                .Select(line => new ReceivingOrderDetailsData.LineInfo(
                    line.Id,
                    new(
                        line.StockKeepingUnitId,
                        line.StockKeepingUnit.Code,
                        line.StockKeepingUnit.Name,
                        new(
                            line.StockKeepingUnit.BaseUnitOfMeasureId,
                            line.StockKeepingUnit.BaseUnitOfMeasure.Code,
                            line.StockKeepingUnit.BaseUnitOfMeasure.Symbol)),
                    line.PlannedQuantity,
                    line.ReceivedQuantity,
                    line.PlannedQuantity - line.ReceivedQuantity))
                .ToList()));
    }

    public static ReceivingOrderDetails ToDetails(this ReceivingOrderDetailsData data) => new(
        data.Id,
        Convert.ToBase64String(data.RowVersion),
        data.Number,
        data.Status,
        data.CreatedAtUtc,
        data.UpdatedAtUtc,
        data.StartedAtUtc,
        data.CompletedAtUtc,
        data.InventoryTransactionId,
        new(data.Warehouse.Id, data.Warehouse.Code, data.Warehouse.Name),
        new(data.ReceivingLocation.Id, data.ReceivingLocation.Code, data.ReceivingLocation.Name),
        data.LineCount,
        data.TotalPlannedQuantity,
        data.TotalReceivedQuantity,
        data.TotalRemainingQuantity,
        data.Lines.Select(line => new ReceivingOrderLineDetails(
            line.Id,
            new(
                line.Sku.Id,
                line.Sku.Code,
                line.Sku.Name,
                new(line.Sku.BaseUom.Id, line.Sku.BaseUom.Code, line.Sku.BaseUom.Symbol)),
            line.PlannedQuantity,
            line.ReceivedQuantity,
            line.RemainingQuantity)).ToList());
}

internal sealed record ReceivingOrderDetailsData(
    Guid Id,
    byte[] RowVersion,
    string Number,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    Guid? InventoryTransactionId,
    ReceivingOrderDetailsData.WarehouseInfo Warehouse,
    ReceivingOrderDetailsData.StorageLocationInfo ReceivingLocation,
    int LineCount,
    decimal TotalPlannedQuantity,
    decimal TotalReceivedQuantity,
    decimal TotalRemainingQuantity,
    IReadOnlyList<ReceivingOrderDetailsData.LineInfo> Lines)
{
    public sealed record WarehouseInfo(Guid Id, string Code, string Name);
    public sealed record StorageLocationInfo(Guid Id, string Code, string Name);
    public sealed record UnitOfMeasureInfo(Guid Id, string Code, string? Symbol);
    public sealed record StockKeepingUnitInfo(
        Guid Id,
        string Code,
        string Name,
        UnitOfMeasureInfo BaseUom);
    public sealed record LineInfo(
        Guid Id,
        StockKeepingUnitInfo Sku,
        decimal PlannedQuantity,
        decimal ReceivedQuantity,
        decimal RemainingQuantity);
}
