using Myrmex.WebApp.Wms.Api;

namespace Myrmex.WebApp.Wms.Catalog;

public sealed class WmsCatalogApiClient(HttpClient httpClient)
{
    public async Task<ListResult<StockKeepingUnitDetails>> ListStockKeepingUnitsAsync(
        ListRequest request,
        CancellationToken cancellationToken = default)
    {
        string url = WmsApiUrls.BuildListUrl(
            "/api/wms/catalog/skus",
            request);

        return await httpClient.GetRequiredAsync<ListResult<StockKeepingUnitDetails>>(url, cancellationToken);
    }

    public async Task<StockKeepingUnitDetails> GetStockKeepingUnitByIdAsync(
        Guid stockKeepingUnitId,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.GetRequiredAsync<StockKeepingUnitDetails>(
            $"/api/wms/catalog/skus/{stockKeepingUnitId}",
            cancellationToken);
    }

    public async Task<ListResult<UnitOfMeasureDetails>> ListUnitsOfMeasureAsync(
        ListRequest request,
        CancellationToken cancellationToken = default)
    {
        string url = WmsApiUrls.BuildListUrl(
            "/api/wms/catalog/uoms",
            request);

        return await httpClient.GetRequiredAsync<ListResult<UnitOfMeasureDetails>>(url, cancellationToken);
    }

    public async Task<UnitOfMeasureDetails> GetUnitOfMeasureByIdAsync(
        Guid unitOfMeasureId,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.GetRequiredAsync<UnitOfMeasureDetails>(
            $"/api/wms/catalog/uoms/{unitOfMeasureId}",
            cancellationToken);
    }

    public async Task<ApiResult<StockKeepingUnitDetails>> TryCreateStockKeepingUnitAsync(
        CreateStockKeepingUnitRequest request,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.PostAsApiResultAsync<StockKeepingUnitDetails>(
            "/api/wms/catalog/skus",
            request,
            cancellationToken);
    }

    public async Task<ApiResult<SkuBarcodeDetails>> TryCreateSkuBarcodeAsync(
        CreateSkuBarcodeRequest request,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.PostAsApiResultAsync<SkuBarcodeDetails>(
            "/api/wms/catalog/sku-barcodes",
            request,
            cancellationToken);
    }

    public async Task<ApiResult<UnitOfMeasureDetails>> TryCreateUnitOfMeasureAsync(
        CreateUnitOfMeasureRequest request,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.PostAsApiResultAsync<UnitOfMeasureDetails>(
            "/api/wms/catalog/uoms",
            request,
            cancellationToken);
    }

    public async Task<ApiResult<UnitOfMeasureDetails>> TryUpdateUnitOfMeasureDetailsAsync(
        Guid unitOfMeasureId,
        UpdateUnitOfMeasureDetailsRequest request,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.PutAsApiResultAsync<UnitOfMeasureDetails>(
            $"/api/wms/catalog/uoms/{unitOfMeasureId}",
            request,
            cancellationToken);
    }

    public async Task<ApiResult<UnitOfMeasureDetails>> TryDeactivateUnitOfMeasureAsync(
        Guid unitOfMeasureId,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.PostAsApiResultAsync<UnitOfMeasureDetails>(
            $"/api/wms/catalog/uoms/{unitOfMeasureId}/deactivate",
            value: null,
            cancellationToken);
    }

    public async Task<ApiResult<UnitOfMeasureDetails>> TryReactivateUnitOfMeasureAsync(
        Guid unitOfMeasureId,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.PostAsApiResultAsync<UnitOfMeasureDetails>(
            $"/api/wms/catalog/uoms/{unitOfMeasureId}/reactivate",
            value: null,
            cancellationToken);
    }

    public async Task<ApiResult<StockKeepingUnitDetails>> TryUpdateStockKeepingUnitDetailsAsync(
        Guid stockKeepingUnitId,
        UpdateStockKeepingUnitDetailsRequest request,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.PutAsApiResultAsync<StockKeepingUnitDetails>(
            $"/api/wms/catalog/skus/{stockKeepingUnitId}",
            request,
            cancellationToken);
    }

    public async Task<ApiResult<StockKeepingUnitDetails>> TryDeactivateStockKeepingUnitAsync(
        Guid stockKeepingUnitId,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.PostAsApiResultAsync<StockKeepingUnitDetails>(
            $"/api/wms/catalog/skus/{stockKeepingUnitId}/deactivate",
            value: null,
            cancellationToken);
    }

    public async Task<ApiResult<StockKeepingUnitDetails>> TryReactivateStockKeepingUnitAsync(
        Guid stockKeepingUnitId,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.PostAsApiResultAsync<StockKeepingUnitDetails>(
            $"/api/wms/catalog/skus/{stockKeepingUnitId}/reactivate",
            value: null,
            cancellationToken);
    }

}

public sealed record StockKeepingUnitDetails(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record CreateStockKeepingUnitRequest(
    string? Code,
    string? Name,
    string? Description);

public sealed record UpdateStockKeepingUnitDetailsRequest(
    string? Name,
    string? Description);

public sealed record UnitOfMeasureDetails(
    Guid Id,
    string Code,
    string Name,
    string? Symbol,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record CreateUnitOfMeasureRequest(
    string? Code,
    string? Name,
    string? Symbol);

public sealed record UpdateUnitOfMeasureDetailsRequest(
    string? Name,
    string? Symbol);

public sealed record SkuBarcodeDetails(
    Guid Id,
    Guid StockKeepingUnitId,
    string Value,
    string Symbology,
    bool IsPrimary,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record CreateSkuBarcodeRequest(
    Guid StockKeepingUnitId,
    string? Value,
    string? Symbology,
    bool IsPrimary);
