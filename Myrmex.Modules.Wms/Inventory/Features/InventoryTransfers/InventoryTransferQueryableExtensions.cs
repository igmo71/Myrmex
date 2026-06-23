using Myrmex.Modules.Wms.Inventory.Domain.InventoryTransfers;
using Myrmex.Shared.Wms.Inventory;

namespace Myrmex.Modules.Wms.Inventory.Features.InventoryTransfers;

internal static class InventoryTransferQueryableExtensions
{
    public static IQueryable<InventoryTransferDetailsData> ProjectDetailsData(this IQueryable<InventoryTransfer> queryable)
    {
        return queryable.Select(transfer => new InventoryTransferDetailsData(
            transfer.Id,
            transfer.Code,
            transfer.Status.ToString(),
            transfer.CreatedAtUtc,
            transfer.UpdatedAtUtc,
            new InventoryTransferDetailsData.WarehouseInfo(
                transfer.SourceWarehouseId,
                transfer.SourceWarehouse.Code,
                transfer.SourceWarehouse.Name),
            new InventoryTransferDetailsData.WarehouseInfo(
                transfer.DestinationWarehouseId,
                transfer.DestinationWarehouse.Code,
                transfer.DestinationWarehouse.Name),
            transfer.TransitStorageLocation == null
                ? null
                : new InventoryTransferDetailsData.StorageLocationInfo(
                    transfer.TransitStorageLocation.Id,
                    transfer.TransitStorageLocation.Code,
                    transfer.TransitStorageLocation.Name),
            transfer.Lines
                .OrderBy(line => line.CreatedAtUtc)
                .Select(line => new InventoryTransferDetailsData.LineInfo(
                    line.Id,
                    line.RequestedQuantity,
                    transfer.Movements
                        .Where(movement => movement.InventoryTransferLineId == line.Id)
                        .Where(movement => movement.FromStorageLocationId == line.SourceStorageLocationId &&
                                           movement.ToStorageLocationId == line.DestinationStorageLocationId)
                        .Sum(movement => movement.Quantity),
                    transfer.Movements
                        .Where(movement => movement.InventoryTransferLineId == line.Id)
                        .Where(movement => movement.FromStorageLocationId == line.SourceStorageLocationId)
                        .Sum(movement => movement.Quantity),
                    transfer.Movements
                        .Where(movement => movement.InventoryTransferLineId == line.Id)
                        .Where(movement => movement.ToStorageLocationId == line.DestinationStorageLocationId)
                        .Sum(movement => movement.Quantity),
                    transfer.Movements
                        .Where(movement => movement.InventoryTransferLineId == line.Id)
                        .Where(movement => movement.FromStorageLocationId == line.SourceStorageLocationId)
                        .Sum(movement => movement.Quantity) -
                    transfer.Movements
                        .Where(movement => movement.InventoryTransferLineId == line.Id)
                        .Where(movement => movement.ToStorageLocationId == line.DestinationStorageLocationId)
                        .Sum(movement => movement.Quantity),
                    new InventoryTransferDetailsData.StockKeepingUnitInfo(
                        line.StockKeepingUnitId,
                        line.StockKeepingUnit.Code,
                        line.StockKeepingUnit.Name,
                        new InventoryTransferDetailsData.UnitOfMeasureInfo(
                            line.StockKeepingUnit.BaseUnitOfMeasureId,
                            line.StockKeepingUnit.BaseUnitOfMeasure.Code,
                            line.StockKeepingUnit.BaseUnitOfMeasure.Symbol)),
                    new InventoryTransferDetailsData.StorageLocationInfo(
                        line.SourceStorageLocationId,
                        line.SourceStorageLocation.Code,
                        line.SourceStorageLocation.Name),
                    new InventoryTransferDetailsData.StorageLocationInfo(
                        line.DestinationStorageLocationId,
                        line.DestinationStorageLocation.Code,
                        line.DestinationStorageLocation.Name)))
                .ToList(),
            transfer.Movements
                .OrderBy(movement => movement.OccurredAtUtc)
                .Select(movement => new InventoryTransferDetailsData.MovementInfo(
                    movement.Id,
                    movement.InventoryTransferLineId,
                    movement.InventoryTransactionId,
                    movement.OccurredAtUtc,
                    movement.Quantity,
                    new InventoryTransferDetailsData.StockKeepingUnitInfo(
                        movement.InventoryTransferLine.StockKeepingUnitId,
                        movement.InventoryTransferLine.StockKeepingUnit.Code,
                        movement.InventoryTransferLine.StockKeepingUnit.Name,
                        new InventoryTransferDetailsData.UnitOfMeasureInfo(
                            movement.InventoryTransferLine.StockKeepingUnit.BaseUnitOfMeasureId,
                            movement.InventoryTransferLine.StockKeepingUnit.BaseUnitOfMeasure.Code,
                            movement.InventoryTransferLine.StockKeepingUnit.BaseUnitOfMeasure.Symbol)),
                    new InventoryTransferDetailsData.StorageLocationInfo(
                        movement.FromStorageLocationId,
                        movement.FromStorageLocation.Code,
                        movement.FromStorageLocation.Name),
                    new InventoryTransferDetailsData.StorageLocationInfo(
                        movement.ToStorageLocationId,
                        movement.ToStorageLocation.Code,
                        movement.ToStorageLocation.Name)))
                .ToList()));
    }

    public static InventoryTransferDetails ToDetails(this InventoryTransferDetailsData data)
    {
        return new InventoryTransferDetails(
            data.Id,
            data.Code,
            data.Status,
            data.CreatedAtUtc,
            data.UpdatedAtUtc,
            new InventoryTransferDetails.WarehouseInfo(
                data.SourceWarehouse.Id,
                data.SourceWarehouse.Code,
                data.SourceWarehouse.Name),
            new InventoryTransferDetails.WarehouseInfo(
                data.DestinationWarehouse.Id,
                data.DestinationWarehouse.Code,
                data.DestinationWarehouse.Name),
            data.TransitStorageLocation is null
                ? null
                : new InventoryTransferDetails.StorageLocationInfo(
                    data.TransitStorageLocation.Id,
                    data.TransitStorageLocation.Code,
                    data.TransitStorageLocation.Name),
            data.Lines.Select(line => new InventoryTransferLineDetails(
                line.Id,
                line.RequestedQuantity,
                line.MovedQuantity,
                line.PickedQuantity,
                line.PlacedQuantity,
                line.InTransitQuantity,
                new InventoryTransferLineDetails.StockKeepingUnitInfo(
                    line.Sku.Id,
                    line.Sku.Code,
                    line.Sku.Name,
                    new InventoryTransferLineDetails.UnitOfMeasureInfo(
                        line.Sku.BaseUom.Id,
                        line.Sku.BaseUom.Code,
                        line.Sku.BaseUom.Symbol)),
                new InventoryTransferLineDetails.StorageLocationInfo(
                    line.SourceStorageLocation.Id,
                    line.SourceStorageLocation.Code,
                    line.SourceStorageLocation.Name),
                new InventoryTransferLineDetails.StorageLocationInfo(
                    line.DestinationStorageLocation.Id,
                    line.DestinationStorageLocation.Code,
                    line.DestinationStorageLocation.Name)))
                .ToList(),
            data.Movements.Select(movement => new InventoryTransferMovementDetails(
                movement.Id,
                movement.LineId,
                movement.InventoryTransactionId,
                movement.OccurredAtUtc,
                movement.Quantity,
                new InventoryTransferMovementDetails.StockKeepingUnitInfo(
                    movement.Sku.Id,
                    movement.Sku.Code,
                    movement.Sku.Name,
                    new InventoryTransferMovementDetails.UnitOfMeasureInfo(
                        movement.Sku.BaseUom.Id,
                        movement.Sku.BaseUom.Code,
                        movement.Sku.BaseUom.Symbol)),
                new InventoryTransferMovementDetails.StorageLocationInfo(
                    movement.FromStorageLocation.Id,
                    movement.FromStorageLocation.Code,
                    movement.FromStorageLocation.Name),
                new InventoryTransferMovementDetails.StorageLocationInfo(
                    movement.ToStorageLocation.Id,
                    movement.ToStorageLocation.Code,
                    movement.ToStorageLocation.Name)))
                .ToList());
    }
}

internal sealed record InventoryTransferDetailsData(
    Guid Id,
    string Code,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    InventoryTransferDetailsData.WarehouseInfo SourceWarehouse,
    InventoryTransferDetailsData.WarehouseInfo DestinationWarehouse,
    InventoryTransferDetailsData.StorageLocationInfo? TransitStorageLocation,
    IReadOnlyList<InventoryTransferDetailsData.LineInfo> Lines,
    IReadOnlyList<InventoryTransferDetailsData.MovementInfo> Movements)
{
    public sealed record WarehouseInfo(Guid Id, string Code, string Name);

    public sealed record StorageLocationInfo(Guid Id, string Code, string Name);

    public sealed record StockKeepingUnitInfo(
        Guid Id,
        string Code,
        string Name,
        UnitOfMeasureInfo BaseUom);

    public sealed record UnitOfMeasureInfo(Guid Id, string Code, string? Symbol);

    public sealed record LineInfo(
        Guid Id,
        decimal RequestedQuantity,
        decimal MovedQuantity,
        decimal PickedQuantity,
        decimal PlacedQuantity,
        decimal InTransitQuantity,
        StockKeepingUnitInfo Sku,
        StorageLocationInfo SourceStorageLocation,
        StorageLocationInfo DestinationStorageLocation);

    public sealed record MovementInfo(
        Guid Id,
        Guid LineId,
        Guid InventoryTransactionId,
        DateTimeOffset OccurredAtUtc,
        decimal Quantity,
        StockKeepingUnitInfo Sku,
        StorageLocationInfo FromStorageLocation,
        StorageLocationInfo ToStorageLocation);
}
