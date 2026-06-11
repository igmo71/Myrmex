namespace Myrmex.Modules.Wms.Inventory.Features.InventoryBalances;

internal sealed record InventoryBalanceDetails(
    Guid Id,
    Guid StockKeepingUnitId,
    string StockKeepingUnitCode,
    string StockKeepingUnitName,
    Guid StorageLocationId,
    string StorageLocationCode,
    string StorageLocationName,
    Guid WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    Guid BaseUnitOfMeasureId,
    string BaseUnitOfMeasureCode,
    string? BaseUnitOfMeasureSymbol,
    decimal Quantity,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);
