using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Inventory;
using Myrmex.WebApp.Wms.Api;
using System.Globalization;
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

    public async Task<ApiResult<InventoryBalanceDetails>> TryAdjustInventoryBalanceAsync(
        AdjustInventoryBalanceRequest request,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.PostAsApiResultAsync<InventoryBalanceDetails>(
            "/api/wms/inventory/adjustments",
            request,
            cancellationToken);
    }

    public async Task<ApiResult<InventoryTransferDetails>> TryCreateInventoryTransferAsync(
        CreateInventoryTransferRequest request,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.PostAsApiResultAsync<InventoryTransferDetails>(
            "/api/wms/inventory/transfers",
            request,
            cancellationToken);
    }

    public async Task<ApiResult<InventoryTransferDetails>> TryMoveInventoryTransferLineAsync(
        Guid transferId,
        Guid lineId,
        MoveInventoryTransferLineRequest request,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.PostAsApiResultAsync<InventoryTransferDetails>(
            $"/api/wms/inventory/transfers/{transferId}/lines/{lineId}/move",
            request,
            cancellationToken);
    }

    public async Task<ApiResult<InventoryTransferDetails>> TryPickInventoryTransferLineAsync(
        Guid transferId,
        Guid lineId,
        PickInventoryTransferLineRequest request,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.PostAsApiResultAsync<InventoryTransferDetails>(
            $"/api/wms/inventory/transfers/{transferId}/lines/{lineId}/pick",
            request,
            cancellationToken);
    }

    public async Task<ApiResult<InventoryTransferDetails>> TryPlaceInventoryTransferLineAsync(
        Guid transferId,
        Guid lineId,
        PlaceInventoryTransferLineRequest request,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.PostAsApiResultAsync<InventoryTransferDetails>(
            $"/api/wms/inventory/transfers/{transferId}/lines/{lineId}/place",
            request,
            cancellationToken);
    }

    public async Task<ListResult<InventoryLedgerEntryDetails>> ListInventoryLedgerEntriesAsync(
        ListInventoryLedgerEntriesRequest request,
        CancellationToken cancellationToken = default)
    {
        string url = BuildInventoryLedgerListUrl(request);

        return await httpClient.GetRequiredAsync<ListResult<InventoryLedgerEntryDetails>>(
            url,
            cancellationToken);
    }

    public async Task<InventoryTransactionDetails> GetInventoryTransactionByIdAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.GetRequiredAsync<InventoryTransactionDetails>(
            $"/api/wms/inventory/transactions/{transactionId}",
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

    private static string BuildInventoryLedgerListUrl(ListInventoryLedgerEntriesRequest request)
    {
        string path = "/api/wms/inventory/ledger";

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

        if (request.WarehouseId.HasValue)
        {
            query.Add($"warehouseId={request.WarehouseId.Value}");
        }

        if (request.StorageLocationId.HasValue)
        {
            query.Add($"storageLocationId={request.StorageLocationId.Value}");
        }

        if (!string.IsNullOrWhiteSpace(request.TransactionType))
        {
            query.Add($"transactionType={HttpUtility.UrlEncode(request.TransactionType)}");
        }

        if (request.OccurredFromUtc.HasValue)
        {
            query.Add(
                $"occurredFromUtc={HttpUtility.UrlEncode(request.OccurredFromUtc.Value.ToString("O", CultureInfo.InvariantCulture))}");
        }

        if (request.OccurredToUtc.HasValue)
        {
            query.Add(
                $"occurredToUtc={HttpUtility.UrlEncode(request.OccurredToUtc.Value.ToString("O", CultureInfo.InvariantCulture))}");
        }

        return query.Count == 0
            ? path
            : $"{path}?{string.Join("&", query)}";
    }
}
