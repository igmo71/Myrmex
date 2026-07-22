using Myrmex.Shared.Wms.Receiving;
using Myrmex.WebApp.Wms.Api;

namespace Myrmex.WebApp.Wms.Receiving;

public sealed class WmsReceivingApiClient(HttpClient httpClient)
{
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
}
