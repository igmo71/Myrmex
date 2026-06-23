using Microsoft.EntityFrameworkCore;
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
                    movement.FromStorageLocationId == movement.InventoryTransferLine.SourceStorageLocationId &&
                    movement.ToStorageLocationId == movement.InventoryTransferLine.DestinationStorageLocationId
                        ? "Direct"
                        : transfer.TransitStorageLocationId != null &&
                          movement.FromStorageLocationId == movement.InventoryTransferLine.SourceStorageLocationId &&
                          movement.ToStorageLocationId == transfer.TransitStorageLocationId.Value
                            ? "Pick"
                            : transfer.TransitStorageLocationId != null &&
                              movement.FromStorageLocationId == transfer.TransitStorageLocationId.Value &&
                              movement.ToStorageLocationId == movement.InventoryTransferLine.DestinationStorageLocationId
                                ? "Place"
                                : "Movement",
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
                line.RequestedQuantity - line.PickedQuantity,
                line.PickedQuantity - line.PlacedQuantity,
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
                movement.MovementMeaning,
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

    public static IQueryable<InventoryTransferListItemData> ProjectListItemData(this IQueryable<InventoryTransfer> queryable)
    {
        return queryable.Select(transfer => new InventoryTransferListItemData(
            transfer.Id,
            transfer.Code,
            transfer.Status.ToString(),
            transfer.CreatedAtUtc,
            transfer.UpdatedAtUtc,
            transfer.Lines.Sum(line => line.RequestedQuantity),
            transfer.Movements
                .Where(movement => movement.FromStorageLocationId == movement.InventoryTransferLine.SourceStorageLocationId)
                .Sum(movement => movement.Quantity),
            transfer.Movements
                .Where(movement => movement.ToStorageLocationId == movement.InventoryTransferLine.DestinationStorageLocationId)
                .Sum(movement => movement.Quantity),
            transfer.Movements
                .Where(movement => movement.FromStorageLocationId == movement.InventoryTransferLine.SourceStorageLocationId)
                .Sum(movement => movement.Quantity) -
            transfer.Movements
                .Where(movement => movement.ToStorageLocationId == movement.InventoryTransferLine.DestinationStorageLocationId)
                .Sum(movement => movement.Quantity),
            new InventoryTransferListItemData.WarehouseInfo(
                transfer.SourceWarehouseId,
                transfer.SourceWarehouse.Code,
                transfer.SourceWarehouse.Name),
            new InventoryTransferListItemData.WarehouseInfo(
                transfer.DestinationWarehouseId,
                transfer.DestinationWarehouse.Code,
                transfer.DestinationWarehouse.Name),
            transfer.TransitStorageLocation == null
                ? null
                : new InventoryTransferListItemData.StorageLocationInfo(
                    transfer.TransitStorageLocation.Id,
                    transfer.TransitStorageLocation.Code,
                    transfer.TransitStorageLocation.Name)));
    }

    public static InventoryTransferListItem ToListItem(this InventoryTransferListItemData data)
    {
        return new InventoryTransferListItem(
            data.Id,
            data.Code,
            data.Status,
            data.CreatedAtUtc,
            data.UpdatedAtUtc,
            data.TotalRequestedQuantity,
            data.TotalPickedQuantity,
            data.TotalPlacedQuantity,
            data.TotalInTransitQuantity,
            new InventoryTransferListItem.WarehouseInfo(
                data.SourceWarehouse.Id,
                data.SourceWarehouse.Code,
                data.SourceWarehouse.Name),
            new InventoryTransferListItem.WarehouseInfo(
                data.DestinationWarehouse.Id,
                data.DestinationWarehouse.Code,
                data.DestinationWarehouse.Name),
            data.TransitStorageLocation is null
                ? null
                : new InventoryTransferListItem.StorageLocationInfo(
                    data.TransitStorageLocation.Id,
                    data.TransitStorageLocation.Code,
                    data.TransitStorageLocation.Name));
    }

    public static IQueryable<InventoryTransfer> ApplyFilters(
        this IQueryable<InventoryTransfer> queryable,
        ListInventoryTransfers.Query query)
    {
        if (query.WarehouseId is Guid warehouseId)
        {
            queryable = queryable.Where(x => x.SourceWarehouseId == warehouseId);
        }

        if (query.Status is InventoryTransferStatus status)
        {
            queryable = queryable.Where(x => x.Status == status);
        }

        if (query.CreatedFromUtc is DateTimeOffset createdFromUtc)
        {
            queryable = queryable.Where(x => x.CreatedAtUtc >= createdFromUtc);
        }

        if (query.CreatedToUtc is DateTimeOffset createdToUtc)
        {
            queryable = queryable.Where(x => x.CreatedAtUtc <= createdToUtc);
        }

        if (!string.IsNullOrWhiteSpace(query.TransferCode))
        {
            string transferCode = query.TransferCode.Trim();
            queryable = queryable.Where(x => EF.Functions.Like(x.Code, $"%{transferCode}%"));
        }

        if (query.SourceStorageLocationId is Guid sourceStorageLocationId)
        {
            queryable = queryable.Where(x => x.Lines.Any(line => line.SourceStorageLocationId == sourceStorageLocationId));
        }

        if (query.DestinationStorageLocationId is Guid destinationStorageLocationId)
        {
            queryable = queryable.Where(x => x.Lines.Any(line => line.DestinationStorageLocationId == destinationStorageLocationId));
        }

        if (query.StockKeepingUnitId is Guid stockKeepingUnitId)
        {
            queryable = queryable.Where(x => x.Lines.Any(line => line.StockKeepingUnitId == stockKeepingUnitId));
        }

        if (query.HasTransitLocation is bool hasTransitLocation)
        {
            queryable = hasTransitLocation
                ? queryable.Where(x => x.TransitStorageLocationId != null)
                : queryable.Where(x => x.TransitStorageLocationId == null);
        }

        return queryable;
    }

    public static IQueryable<InventoryTransfer> ApplySorting(
        this IQueryable<InventoryTransfer> queryable,
        string? sortBy,
        bool sortDescending)
    {
        if (sortBy == InventoryTransferSortBy.Code)
            return sortDescending
                ? queryable.OrderByDescending(x => x.Code).ThenBy(x => x.Id)
                : queryable.OrderBy(x => x.Code).ThenBy(x => x.Id);

        if (sortBy == InventoryTransferSortBy.Status)
            return sortDescending
                ? queryable.OrderByDescending(x => x.Status).ThenBy(x => x.Id)
                : queryable.OrderBy(x => x.Status).ThenBy(x => x.Id);

        if (sortBy == InventoryTransferSortBy.WarehouseCode)
            return sortDescending
                ? queryable.OrderByDescending(x => x.SourceWarehouse.Code).ThenBy(x => x.Id)
                : queryable.OrderBy(x => x.SourceWarehouse.Code).ThenBy(x => x.Id);

        if (sortBy == InventoryTransferSortBy.TotalRequestedQuantity)
            return sortDescending
                ? queryable.OrderByDescending(x => x.Lines.Sum(line => line.RequestedQuantity)).ThenBy(x => x.Id)
                : queryable.OrderBy(x => x.Lines.Sum(line => line.RequestedQuantity)).ThenBy(x => x.Id);

        if (sortBy == InventoryTransferSortBy.TotalPickedQuantity)
            return sortDescending
                ? queryable.OrderByDescending(x => x.Movements
                    .Where(movement => movement.FromStorageLocationId == movement.InventoryTransferLine.SourceStorageLocationId)
                    .Sum(movement => movement.Quantity)).ThenBy(x => x.Id)
                : queryable.OrderBy(x => x.Movements
                    .Where(movement => movement.FromStorageLocationId == movement.InventoryTransferLine.SourceStorageLocationId)
                    .Sum(movement => movement.Quantity)).ThenBy(x => x.Id);

        if (sortBy == InventoryTransferSortBy.TotalPlacedQuantity)
            return sortDescending
                ? queryable.OrderByDescending(x => x.Movements
                    .Where(movement => movement.ToStorageLocationId == movement.InventoryTransferLine.DestinationStorageLocationId)
                    .Sum(movement => movement.Quantity)).ThenBy(x => x.Id)
                : queryable.OrderBy(x => x.Movements
                    .Where(movement => movement.ToStorageLocationId == movement.InventoryTransferLine.DestinationStorageLocationId)
                    .Sum(movement => movement.Quantity)).ThenBy(x => x.Id);

        if (sortBy == InventoryTransferSortBy.TotalInTransitQuantity)
            return sortDescending
                ? queryable.OrderByDescending(x =>
                    x.Movements
                        .Where(movement => movement.FromStorageLocationId == movement.InventoryTransferLine.SourceStorageLocationId)
                        .Sum(movement => movement.Quantity) -
                    x.Movements
                        .Where(movement => movement.ToStorageLocationId == movement.InventoryTransferLine.DestinationStorageLocationId)
                        .Sum(movement => movement.Quantity)).ThenBy(x => x.Id)
                : queryable.OrderBy(x =>
                    x.Movements
                        .Where(movement => movement.FromStorageLocationId == movement.InventoryTransferLine.SourceStorageLocationId)
                        .Sum(movement => movement.Quantity) -
                    x.Movements
                        .Where(movement => movement.ToStorageLocationId == movement.InventoryTransferLine.DestinationStorageLocationId)
                        .Sum(movement => movement.Quantity)).ThenBy(x => x.Id);

        return sortDescending
            ? queryable.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id)
            : queryable.OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id);
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
        string MovementMeaning,
        StockKeepingUnitInfo Sku,
        StorageLocationInfo FromStorageLocation,
        StorageLocationInfo ToStorageLocation);
}

internal sealed record InventoryTransferListItemData(
    Guid Id,
    string Code,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    decimal TotalRequestedQuantity,
    decimal TotalPickedQuantity,
    decimal TotalPlacedQuantity,
    decimal TotalInTransitQuantity,
    InventoryTransferListItemData.WarehouseInfo SourceWarehouse,
    InventoryTransferListItemData.WarehouseInfo DestinationWarehouse,
    InventoryTransferListItemData.StorageLocationInfo? TransitStorageLocation)
{
    public sealed record WarehouseInfo(Guid Id, string Code, string Name);

    public sealed record StorageLocationInfo(Guid Id, string Code, string Name);
}
