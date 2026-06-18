using Myrmex.Shared.Common;
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

    public async Task<ApiResult<InventoryBalanceDetails>> TryAdjustInventoryBalanceAsync(
        AdjustInventoryBalanceRequest request,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.PostAsApiResultAsync<InventoryBalanceDetails>(
            "/api/wms/inventory/adjustments",
            request,
            cancellationToken);
    }

    private static string BuildInventoryBalanceListUrl(ListInventoryBalancesRequest request)
    {
        string path = "/api/wms/inventory/balances";

        List<string> query = [];

        if (request.Skip.HasValue)
        {
            query.Add($"skip={request.Skip.Value}");
        }

        if (request.Take.HasValue)
        {
            query.Add($"take={request.Take.Value}");
        }

        if (!string.IsNullOrWhiteSpace(request.SortBy))
        {
            query.Add($"sortBy={HttpUtility.UrlEncode(request.SortBy)}");
        }

        if (request.SortDescending.HasValue)
        {
            query.Add($"sortDescending={request.SortDescending.Value.ToString().ToLowerInvariant()}");
        }

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

        return query.Count == 0
            ? path
            : $"{path}?{string.Join("&", query)}";
    }
}
