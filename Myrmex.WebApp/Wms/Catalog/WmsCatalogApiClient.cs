using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Catalog;
using Myrmex.WebApp.Wms.Api;
using System.Web;

namespace Myrmex.WebApp.Wms.Catalog;

public sealed class WmsCatalogApiClient(HttpClient httpClient)
{
    public async Task<ListResult<StockKeepingUnitDetails>> ListStockKeepingUnitsAsync(
        ListStockKeepingUnitsRequest request,
        CancellationToken cancellationToken = default)
    {
        string url = BuildStockKeepingUnitListUrl(request);

        return await httpClient.GetRequiredAsync<ListResult<StockKeepingUnitDetails>>(url, cancellationToken);
    }

    public async Task<IReadOnlyList<StockKeepingUnitLookupItem>> LookupStockKeepingUnitsAsync(
        LookupStockKeepingUnitsRequest request,
        CancellationToken cancellationToken = default)
    {
        string url = BuildStockKeepingUnitLookupUrl(request);

        return await httpClient.GetRequiredAsync<IReadOnlyList<StockKeepingUnitLookupItem>>(
            url,
            cancellationToken);
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
        ListUnitsOfMeasureRequest request,
        CancellationToken cancellationToken = default)
    {
        string url = BuildUnitOfMeasureListUrl(request);

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

    public async Task<ListResult<SkuBarcodeDetails>> ListSkuBarcodesAsync(
        ListSkuBarcodesRequest request,
        CancellationToken cancellationToken = default)
    {
        string url = BuildSkuBarcodeListUrl(request);

        return await httpClient.GetRequiredAsync<ListResult<SkuBarcodeDetails>>(url, cancellationToken);
    }

    public async Task<SkuBarcodeDetails> GetSkuBarcodeByIdAsync(
        Guid skuBarcodeId,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.GetRequiredAsync<SkuBarcodeDetails>(
            $"/api/wms/catalog/sku-barcodes/{skuBarcodeId}",
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

    public async Task<ApiResult<SkuBarcodeDetails>> TryUpdateSkuBarcodeDetailsAsync(
        Guid skuBarcodeId,
        UpdateSkuBarcodeDetailsRequest request,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.PutAsApiResultAsync<SkuBarcodeDetails>(
            $"/api/wms/catalog/sku-barcodes/{skuBarcodeId}",
            request,
            cancellationToken);
    }

    public async Task<ApiResult<SkuBarcodeDetails>> TryDeactivateSkuBarcodeAsync(
        Guid skuBarcodeId,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.PostAsApiResultAsync<SkuBarcodeDetails>(
            $"/api/wms/catalog/sku-barcodes/{skuBarcodeId}/deactivate",
            value: null,
            cancellationToken);
    }

    public async Task<ApiResult<SkuBarcodeDetails>> TryReactivateSkuBarcodeAsync(
        Guid skuBarcodeId,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.PostAsApiResultAsync<SkuBarcodeDetails>(
            $"/api/wms/catalog/sku-barcodes/{skuBarcodeId}/reactivate",
            value: null,
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

    private static string BuildSkuBarcodeListUrl(ListSkuBarcodesRequest request)
    {
        ListRequest listRequest = new(
            request.Skip,
            request.Take,
            request.SearchText,
            request.SortBy,
            request.SortDescending,
            request.IncludeInactive);

        string url = WmsApiUrls.BuildListUrl(
            "/api/wms/catalog/sku-barcodes",
            listRequest);

        if (request.StockKeepingUnitId.HasValue)
        {
            url += $"&stockKeepingUnitId={HttpUtility.UrlEncode(request.StockKeepingUnitId.Value.ToString())}";
        }

        return url;
    }

    private static string BuildStockKeepingUnitListUrl(ListStockKeepingUnitsRequest request)
    {
        return BuildCatalogListUrl(
            "/api/wms/catalog/skus",
            request.Skip,
            request.Take,
            request.SearchText,
            request.SortBy,
            request.SortDescending,
            request.IncludeInactive);
    }

    private static string BuildUnitOfMeasureListUrl(ListUnitsOfMeasureRequest request)
    {
        return BuildCatalogListUrl(
            "/api/wms/catalog/uoms",
            request.Skip,
            request.Take,
            request.SearchText,
            request.SortBy,
            request.SortDescending,
            request.IncludeInactive);
    }

    private static string BuildCatalogListUrl(
        string path,
        int? skip,
        int? take,
        string? searchText,
        string? sortBy,
        bool? sortDescending,
        bool? includeInactive)
    {
        List<string> query = [];

        if (skip.HasValue)
        {
            query.Add($"skip={skip.Value}");
        }

        if (take.HasValue)
        {
            query.Add($"take={take.Value}");
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            query.Add($"searchText={HttpUtility.UrlEncode(searchText)}");
        }

        if (!string.IsNullOrWhiteSpace(sortBy))
        {
            query.Add($"sortBy={HttpUtility.UrlEncode(sortBy)}");
        }

        if (sortDescending.HasValue)
        {
            query.Add($"sortDescending={sortDescending.Value.ToString().ToLowerInvariant()}");
        }

        if (includeInactive.HasValue)
        {
            query.Add($"includeInactive={includeInactive.Value.ToString().ToLowerInvariant()}");
        }

        return query.Count == 0
            ? path
            : $"{path}?{string.Join("&", query)}";
    }

    private static string BuildStockKeepingUnitLookupUrl(LookupStockKeepingUnitsRequest request)
    {
        const string path = "/api/wms/catalog/skus/lookup";

        List<string> query = [];

        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            query.Add($"searchText={HttpUtility.UrlEncode(request.SearchText)}");
        }

        if (request.Take.HasValue)
        {
            query.Add($"take={request.Take.Value}");
        }

        query.Add($"selectableOnly={request.SelectableOnly.ToString().ToLowerInvariant()}");

        return query.Count == 0
            ? path
            : $"{path}?{string.Join("&", query)}";
    }
}

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

public sealed record UpdateSkuBarcodeDetailsRequest(
    string? Value,
    string? Symbology,
    bool IsPrimary);

public sealed record ListSkuBarcodesRequest(
    int Skip = 0,
    int Take = 20,
    string? SearchText = null,
    string? SortBy = null,
    bool SortDescending = false,
    bool IncludeInactive = false,
    Guid? StockKeepingUnitId = null);
