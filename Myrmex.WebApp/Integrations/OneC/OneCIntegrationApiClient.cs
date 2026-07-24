using Myrmex.Shared.Integrations.OneC;
using Myrmex.WebApp.Wms.Api;

namespace Myrmex.WebApp.Integrations.OneC;

public sealed class OneCIntegrationApiClient(HttpClient httpClient)
{
    public Task<ApiResult<OneCConnectionTestResponse>> TestConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        return httpClient.PostAsApiResultAsync<OneCConnectionTestResponse>(
            "/api/integrations/1c/connection/test",
            cancellationToken);
    }

    public Task<ApiResult<OneCImportResponse>> ImportWarehousesAsync(
        CancellationToken cancellationToken = default) =>
        httpClient.PostAsApiResultAsync<OneCImportResponse>(
            "/api/integrations/1c/warehouses/import",
            cancellationToken);

    public Task<ApiResult<OneCImportResponse>> ImportUnitsOfMeasureAsync(
        CancellationToken cancellationToken = default) =>
        httpClient.PostAsApiResultAsync<OneCImportResponse>(
            "/api/integrations/1c/uoms/import",
            cancellationToken);

    public Task<ApiResult<OneCImportResponse>> ImportStockKeepingUnitsAsync(
        CancellationToken cancellationToken = default) =>
        httpClient.PostAsApiResultAsync<OneCImportResponse>(
            "/api/integrations/1c/skus/import",
            cancellationToken);

    public Task<ApiResult<ReceivingOrderImportResponse>> ImportReceivingOrdersAsync(
        ReceivingOrderImportRequest request,
        CancellationToken cancellationToken = default) =>
        httpClient.PostAsApiResultAsync<ReceivingOrderImportResponse>(
            "/api/integrations/1c/receiving-orders/import",
            request,
            cancellationToken);
}
