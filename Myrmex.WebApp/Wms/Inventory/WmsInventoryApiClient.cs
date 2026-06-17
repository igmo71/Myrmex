using Myrmex.Shared.Wms.Inventory;
using Myrmex.WebApp.Wms.Api;
using System.Web;

namespace Myrmex.WebApp.Wms.Inventory;

public sealed class WmsInventoryApiClient(HttpClient httpClient)
{
    public async Task<ListResult<InventoryBalanceDetails>> ListInventoryBalancesAsync(
        ListInventoryBalancesRequest request,
        CancellationToken cancellationToken = default)
    {
        string url = BuildInventoryBalanceListUrl(request);

        return await httpClient.GetRequiredAsync<ListResult<InventoryBalanceDetails>>(
            url,
            cancellationToken);
    }

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

    public async Task<ApiResult<InventoryBalanceDetails>> TryUpdateInventoryBalanceQuantityAsync(
        Guid inventoryBalanceId,
        UpdateInventoryBalanceQuantityRequest request,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.PutAsApiResultAsync<InventoryBalanceDetails>(
            $"/api/wms/inventory/balances/{inventoryBalanceId}/quantity",
            request,
            cancellationToken);
    }

    private static string BuildInventoryBalanceListUrl(ListInventoryBalancesRequest request)
    {
        List<string> query =
        [
            $"skip={request.Skip}",
            $"take={request.Take}"
        ];

        if (!string.IsNullOrWhiteSpace(request.SortBy))
        {
            query.Add($"sortBy={HttpUtility.UrlEncode(request.SortBy)}");
        }

        query.Add($"sortDescending={request.SortDescending.ToString().ToLowerInvariant()}");

        if (request.StockKeepingUnitId.HasValue)
        {
            query.Add($"stockKeepingUnitId={request.StockKeepingUnitId.Value}");
        }

        if (request.StorageLocationId.HasValue)
        {
            query.Add($"storageLocationId={request.StorageLocationId.Value}");
        }

        if (request.WarehouseId.HasValue)
        {
            query.Add($"warehouseId={request.WarehouseId.Value}");
        }

        return $"/api/wms/inventory/balances?{string.Join("&", query)}";
    }
}

public sealed record ListInventoryBalancesRequest(
    int Skip = 0,
    int Take = 20,
    string? SortBy = null,
    bool SortDescending = false,
    Guid? StockKeepingUnitId = null,
    Guid? StorageLocationId = null,
    Guid? WarehouseId = null);
