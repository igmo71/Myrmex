using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Receiving;
using Myrmex.WebApp.Wms.Api;
using System.Web;

namespace Myrmex.WebApp.Wms.Receiving;

public sealed class WmsReceivingApiClient(HttpClient httpClient)
{
    public Task<ListResult<ReceivingOrderListItem>> ListReceivingOrdersAsync(
        ReceivingOrderListRequest request,
        CancellationToken cancellationToken = default) =>
        httpClient.GetRequiredAsync<ListResult<ReceivingOrderListItem>>(
            BuildReceivingOrderListUrl(request),
            cancellationToken);

    public Task<ReceivingOrderDetails> GetReceivingOrderByIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default) =>
        httpClient.GetRequiredAsync<ReceivingOrderDetails>(
            $"/api/wms/receiving-orders/{orderId}", cancellationToken);

    public Task<ApiResult<ReceivingOrderDetails>> TryCreateReceivingOrderAsync(
        CreateReceivingOrderRequest request,
        CancellationToken cancellationToken = default) =>
        httpClient.PostAsApiResultAsync<ReceivingOrderDetails>(
            "/api/wms/receiving-orders", request, cancellationToken);

    public Task<ApiResult<ReceivingOrderDetails>> TryUpdateReceivingOrderDraftAsync(
        Guid orderId,
        UpdateReceivingOrderDraftRequest request,
        CancellationToken cancellationToken = default) =>
        httpClient.PutAsApiResultAsync<ReceivingOrderDetails>(
            $"/api/wms/receiving-orders/{orderId}", request, cancellationToken);

    public Task<ApiResult<bool>> TryDeleteReceivingOrderDraftAsync(
        Guid orderId,
        string expectedOrderVersion,
        CancellationToken cancellationToken = default)
    {
        string encodedVersion = HttpUtility.UrlEncode(expectedOrderVersion);
        return httpClient.DeleteAsApiResultAsync(
            $"/api/wms/receiving-orders/{orderId}?expectedOrderVersion={encodedVersion}",
            cancellationToken);
    }

    public Task<ApiResult<ReceivingOrderDetails>> TryStartReceivingOrderAsync(
        Guid orderId,
        ReceivingOrderActionRequest request,
        CancellationToken cancellationToken = default) =>
        httpClient.PostAsApiResultAsync<ReceivingOrderDetails>(
            $"/api/wms/receiving-orders/{orderId}/start", request, cancellationToken);

    public Task<ApiResult<ReceivingOrderDetails>> TryReceiveLineAsync(
        Guid orderId,
        Guid lineId,
        ReceiveReceivingOrderLineRequest request,
        CancellationToken cancellationToken = default) =>
        httpClient.PostAsApiResultAsync<ReceivingOrderDetails>(
            $"/api/wms/receiving-orders/{orderId}/lines/{lineId}/receive",
            request,
            cancellationToken);

    public Task<ApiResult<ReceivingOrderDetails>> TryCompleteReceivingOrderAsync(
        Guid orderId,
        ReceivingOrderActionRequest request,
        CancellationToken cancellationToken = default) =>
        httpClient.PostAsApiResultAsync<ReceivingOrderDetails>(
            $"/api/wms/receiving-orders/{orderId}/complete", request, cancellationToken);

    private static string BuildReceivingOrderListUrl(ReceivingOrderListRequest request)
    {
        const string path = "/api/wms/receiving-orders";
        List<string> query = [];

        if (request.Skip.HasValue)
        {
            query.Add($"skip={request.Skip.Value}");
        }

        if (request.Take.HasValue)
        {
            query.Add($"take={request.Take.Value}");
        }

        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            query.Add($"searchText={HttpUtility.UrlEncode(request.SearchText)}");
        }

        if (request.WarehouseId.HasValue)
        {
            query.Add($"warehouseId={request.WarehouseId.Value}");
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query.Add($"status={HttpUtility.UrlEncode(request.Status)}");
        }

        if (!string.IsNullOrWhiteSpace(request.SortBy))
        {
            query.Add($"sortBy={HttpUtility.UrlEncode(request.SortBy)}");
        }

        if (request.SortDescending.HasValue)
        {
            query.Add(
                $"sortDescending={request.SortDescending.Value.ToString().ToLowerInvariant()}");
        }

        return query.Count == 0
            ? path
            : $"{path}?{string.Join("&", query)}";
    }
}
