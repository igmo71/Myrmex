using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Inventory;
using Myrmex.WebApp.Wms.Api;
using System.Globalization;
using System.Web;

namespace Myrmex.WebApp.Wms.Inventory;

public sealed class WmsInventoryApiClient(HttpClient httpClient)
{
    public async Task<ApiResult<InventoryCountDetails>> TryCreateInventoryCountAsync(
        CreateInventoryCountRequest request,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.PostAsApiResultAsync<InventoryCountDetails>(
            "/api/wms/inventory/counts",
            request,
            cancellationToken);
    }

    public async Task<InventoryCountDetails> GetInventoryCountByIdAsync(
        Guid inventoryCountId,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.GetRequiredAsync<InventoryCountDetails>(
            $"/api/wms/inventory/counts/{inventoryCountId}",
            cancellationToken);
    }

    public async Task<ApiResult<InventoryCountDetails>> TryAddInventoryCountLineAsync(
        Guid inventoryCountId,
        AddInventoryCountLineRequest request,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.PostAsApiResultAsync<InventoryCountDetails>(
            $"/api/wms/inventory/counts/{inventoryCountId}/lines",
            request,
            cancellationToken);
    }

    public async Task<ApiResult<InventoryCountDetails>> TryRemoveInventoryCountLineAsync(
        Guid inventoryCountId,
        Guid lineId,
        string expectedLineVersion,
        CancellationToken cancellationToken = default)
    {
        string url =
            $"/api/wms/inventory/counts/{inventoryCountId}/lines/{lineId}" +
            $"?expectedLineVersion={HttpUtility.UrlEncode(expectedLineVersion)}";

        return await httpClient.DeleteAsApiResultAsync<InventoryCountDetails>(
            url,
            cancellationToken);
    }

    public async Task<ApiResult<InventoryCountDetails>> TryRecordInventoryCountLineAsync(
        Guid inventoryCountId,
        Guid lineId,
        RecordInventoryCountLineRequest request,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.PostAsApiResultAsync<InventoryCountDetails>(
            $"/api/wms/inventory/counts/{inventoryCountId}/lines/{lineId}/count",
            request,
            cancellationToken);
    }

    public async Task<ApiResult<InventoryCountDetails>> TryApplyInventoryCountLineAsync(
        Guid inventoryCountId,
        Guid lineId,
        ApplyInventoryCountLineRequest request,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.PostAsApiResultAsync<InventoryCountDetails>(
            $"/api/wms/inventory/counts/{inventoryCountId}/lines/{lineId}/apply",
            request,
            cancellationToken);
    }

    public async Task<ApiResult<InventoryCountDetails>> TrySupersedeInventoryCountLineAsync(
        Guid inventoryCountId,
        Guid lineId,
        SupersedeInventoryCountLineRequest request,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.PostAsApiResultAsync<InventoryCountDetails>(
            $"/api/wms/inventory/counts/{inventoryCountId}/lines/{lineId}/supersede",
            request,
            cancellationToken);
    }

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

    public async Task<InventoryBalanceDetails> GetInventoryBalanceBySkuAndStorageLocationAsync(
        Guid stockKeepingUnitId,
        Guid storageLocationId,
        CancellationToken cancellationToken = default)
    {
        string url =
            $"/api/wms/inventory/balances/lookup?skuId={stockKeepingUnitId}" +
            $"&storageLocationId={storageLocationId}";

        return await httpClient.GetRequiredAsync<InventoryBalanceDetails>(
            url,
            cancellationToken);
    }

    public async Task<ApiResult<MoveInventoryBalanceResult>> TryMoveInventoryBalanceAsync(
        MoveInventoryBalanceRequest request,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.PostAsApiResultAsync<MoveInventoryBalanceResult>(
            "/api/wms/inventory/balances/move",
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

    public async Task<ListResult<InventoryTransferListItem>> ListInventoryTransfersAsync(
        ListInventoryTransfersRequest request,
        CancellationToken cancellationToken = default)
    {
        string url = BuildInventoryTransferListUrl(request);

        return await httpClient.GetRequiredAsync<ListResult<InventoryTransferListItem>>(
            url,
            cancellationToken);
    }

    public async Task<InventoryTransferDetails> GetInventoryTransferByIdAsync(
        Guid transferId,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.GetRequiredAsync<InventoryTransferDetails>(
            $"/api/wms/inventory/transfers/{transferId}",
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

    private static string BuildInventoryTransferListUrl(ListInventoryTransfersRequest request)
    {
        string path = "/api/wms/inventory/transfers";

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

        if (request.WarehouseId.HasValue)
        {
            query.Add($"warehouseId={request.WarehouseId.Value}");
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query.Add($"status={HttpUtility.UrlEncode(request.Status)}");
        }

        if (request.CreatedFromUtc.HasValue)
        {
            query.Add(
                $"createdFromUtc={HttpUtility.UrlEncode(request.CreatedFromUtc.Value.ToString("O", CultureInfo.InvariantCulture))}");
        }

        if (request.CreatedToUtc.HasValue)
        {
            query.Add(
                $"createdToUtc={HttpUtility.UrlEncode(request.CreatedToUtc.Value.ToString("O", CultureInfo.InvariantCulture))}");
        }

        if (!string.IsNullOrWhiteSpace(request.TransferCode))
        {
            query.Add($"transferCode={HttpUtility.UrlEncode(request.TransferCode)}");
        }

        if (request.SourceStorageLocationId.HasValue)
        {
            query.Add($"sourceStorageLocationId={request.SourceStorageLocationId.Value}");
        }

        if (request.DestinationStorageLocationId.HasValue)
        {
            query.Add($"destinationStorageLocationId={request.DestinationStorageLocationId.Value}");
        }

        if (request.StockKeepingUnitId.HasValue)
        {
            query.Add($"stockKeepingUnitId={request.StockKeepingUnitId.Value}");
        }

        if (request.HasTransitLocation.HasValue)
        {
            query.Add($"hasTransitLocation={request.HasTransitLocation.Value.ToString().ToLowerInvariant()}");
        }

        return query.Count == 0
            ? path
            : $"{path}?{string.Join("&", query)}";
    }
}
