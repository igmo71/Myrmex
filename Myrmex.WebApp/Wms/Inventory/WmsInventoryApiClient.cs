using Myrmex.WebApp.Wms.Api;

namespace Myrmex.WebApp.Wms.Inventory;

public sealed class WmsInventoryApiClient(HttpClient httpClient)
{
    public async Task<InventoryBalanceDetails> GetInventoryBalanceByIdAsync(
        Guid inventoryBalanceId,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.GetRequiredAsync<InventoryBalanceDetails>(
            $"/api/wms/inventory/balances/{inventoryBalanceId}",
            cancellationToken);
    }

    public async Task<ApiResult<InventoryBalanceDetails>> TryCreateInventoryBalanceAsync(
        CreateInventoryBalanceRequest request,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.PostAsApiResultAsync<InventoryBalanceDetails>(
            "/api/wms/inventory/balances",
            request,
            cancellationToken);
    }
}

public sealed record InventoryBalanceDetails(
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

public sealed record CreateInventoryBalanceRequest(
    Guid? StockKeepingUnitId,
    Guid? StorageLocationId,
    decimal Quantity);
