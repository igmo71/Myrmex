namespace Myrmex.Shared.Wms.Inventory;

public sealed record InventoryTransferLineDetails(
    Guid Id,
    decimal RequestedQuantity,
    decimal MovedQuantity,
    decimal PickedQuantity,
    decimal PlacedQuantity,
    decimal InTransitQuantity,
    InventoryTransferLineDetails.StockKeepingUnitInfo Sku,
    InventoryTransferLineDetails.StorageLocationInfo SourceStorageLocation,
    InventoryTransferLineDetails.StorageLocationInfo DestinationStorageLocation)
{
    public sealed record StockKeepingUnitInfo(
        Guid Id,
        string Code,
        string Name,
        UnitOfMeasureInfo BaseUom);

    public sealed record UnitOfMeasureInfo(Guid Id, string Code, string? Symbol);

    public sealed record StorageLocationInfo(Guid Id, string Code, string Name);
}
