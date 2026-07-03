using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Catalog;
using Myrmex.WebApp.Wms.Api;
using Myrmex.WebApp.Wms.Catalog;
using System.Text;
using System.Text.Json;

namespace Myrmex.Tests.Wms.Catalog.Client;

public sealed class WmsCatalogApiClientTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task ListStockKeepingUnitsAsync_WhenSuccessful_ReturnsBaseUnitOfMeasureIdForEachSku()
    {
        StockKeepingUnitDetails firstDetails = CreateStockKeepingUnitDetails(
            id: Guid.Parse("018f0000-0000-7000-8000-000000000001"),
            code: "ITEM-001",
            baseUnitOfMeasureId: Guid.Parse("018f0000-0000-7000-8000-000000000111"));
        StockKeepingUnitDetails secondDetails = CreateStockKeepingUnitDetails(
            id: Guid.Parse("018f0000-0000-7000-8000-000000000002"),
            code: "ITEM-002",
            baseUnitOfMeasureId: Guid.Parse("018f0000-0000-7000-8000-000000000222"),
            isActive: false,
            updatedAtUtc: DateTimeOffset.Parse("2026-06-10T01:00:00Z"));

        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson(new ListResult<StockKeepingUnitDetails>(
                [firstDetails, secondDetails],
                TotalCount: 2,
                Skip: 0,
                Take: 20)),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsCatalogApiClient apiClient = new(httpClient);
        using CancellationTokenSource cancellationTokenSource = new();

        ListResult<StockKeepingUnitDetails> result = await apiClient.ListStockKeepingUnitsAsync(
            new ListStockKeepingUnitsRequest
            {
                Skip = 5,
                Take = 25,
                SearchText = "Widget & Part",
                SortBy = StockKeepingUnitSortBy.CreatedAtUtc,
                SortDescending = true,
                IncludeInactive = true
            },
            cancellationTokenSource.Token);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(
            [firstDetails.BaseUnitOfMeasureId, secondDetails.BaseUnitOfMeasureId],
            result.Items.Select(x => x.BaseUnitOfMeasureId).ToArray());
        Assert.Equal(HttpMethod.Get, handler.RequestMethod);
        Assert.Equal("/api/wms/catalog/skus", handler.RequestPath);
        Assert.Equal(
            "?skip=5&take=25&searchText=Widget+%26+Part&sortBy=CreatedAtUtc" +
            "&sortDescending=true&includeInactive=true",
            handler.RequestQuery);
        Assert.True(handler.RequestCancellationToken.CanBeCanceled);
    }

    [Fact]
    public async Task GetStockKeepingUnitByIdAsync_WhenSuccessful_ReturnsBaseUnitOfMeasureId()
    {
        Guid stockKeepingUnitId = Guid.Parse("018f0000-0000-7000-8000-000000000001");
        StockKeepingUnitDetails details = CreateStockKeepingUnitDetails(id: stockKeepingUnitId);

        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson(details),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsCatalogApiClient apiClient = new(httpClient);

        StockKeepingUnitDetails result = await apiClient.GetStockKeepingUnitByIdAsync(
            stockKeepingUnitId,
            TestContext.Current.CancellationToken);

        Assert.Equal(stockKeepingUnitId, result.Id);
        Assert.Equal(details.BaseUnitOfMeasureId, result.BaseUnitOfMeasureId);
        Assert.Equal(HttpMethod.Get, handler.RequestMethod);
        Assert.Equal($"/api/wms/catalog/skus/{stockKeepingUnitId}", handler.RequestPath);
    }

    [Fact]
    public async Task LookupStockKeepingUnitsAsync_WhenSuccessful_BuildsLookupRoute()
    {
        StockKeepingUnitLookupItem details = new(
            Id: Guid.Parse("018f0000-0000-7000-8000-000000000001"),
            Code: "ITEM-001",
            Name: "Widget",
            BaseUnitOfMeasureId: Guid.Parse("018f0000-0000-7000-8000-000000000111"),
            BaseUnitOfMeasureCode: "EA",
            BaseUnitOfMeasureSymbol: "ea",
            IsActive: false,
            IsBaseUnitOfMeasureActive: false);

        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson<IReadOnlyList<StockKeepingUnitLookupItem>>([details]),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsCatalogApiClient apiClient = new(httpClient);
        using CancellationTokenSource cancellationTokenSource = new();

        IReadOnlyList<StockKeepingUnitLookupItem> result = await apiClient.LookupStockKeepingUnitsAsync(
            new LookupStockKeepingUnitsRequest
            {
                SearchText = "Widget",
                Take = 20,
                SelectableOnly = false
            },
            cancellationTokenSource.Token);

        StockKeepingUnitLookupItem item = Assert.Single(result);
        Assert.Equal(details.Id, item.Id);
        Assert.False(item.IsActive);
        Assert.False(item.IsBaseUnitOfMeasureActive);
        Assert.Equal(HttpMethod.Get, handler.RequestMethod);
        Assert.Equal("/api/wms/catalog/skus/lookup", handler.RequestPath);
        Assert.Equal("?searchText=Widget&take=20&selectableOnly=false", handler.RequestQuery);
    }

    [Fact]
    public async Task TryCreateStockKeepingUnitAsync_WhenSuccessful_PostsBaseUnitOfMeasureIdAndReturnsDetails()
    {
        Guid baseUnitOfMeasureId = Guid.Parse("018f0000-0000-7000-8000-000000000111");
        StockKeepingUnitDetails details = CreateStockKeepingUnitDetails(baseUnitOfMeasureId: baseUnitOfMeasureId);

        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson(details),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsCatalogApiClient apiClient = new(httpClient);

        ApiResult<StockKeepingUnitDetails> result = await apiClient.TryCreateStockKeepingUnitAsync(
            new CreateStockKeepingUnitRequest(
                Code: "ITEM-001",
                Name: "Widget",
                Description: "Sellable widget",
                BaseUnitOfMeasureId: baseUnitOfMeasureId),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(details.Id, result.Value.Id);
        Assert.Equal(baseUnitOfMeasureId, result.Value.BaseUnitOfMeasureId);
        Assert.Equal(HttpMethod.Post, handler.RequestMethod);
        Assert.Equal("/api/wms/catalog/skus", handler.RequestPath);

        using JsonDocument requestBody = JsonDocument.Parse(handler.RequestBody);
        JsonElement root = requestBody.RootElement;
        Assert.Equal("ITEM-001", root.GetProperty("code").GetString());
        Assert.Equal("Widget", root.GetProperty("name").GetString());
        Assert.Equal("Sellable widget", root.GetProperty("description").GetString());
        Assert.Equal(baseUnitOfMeasureId, root.GetProperty("baseUnitOfMeasureId").GetGuid());
    }

    [Fact]
    public async Task TryUpdateStockKeepingUnitDetailsAsync_WhenSuccessful_PutsBaseUnitOfMeasureIdAndReturnsDetails()
    {
        Guid stockKeepingUnitId = Guid.Parse("018f0000-0000-7000-8000-000000000001");
        Guid baseUnitOfMeasureId = Guid.Parse("018f0000-0000-7000-8000-000000000222");
        StockKeepingUnitDetails details = CreateStockKeepingUnitDetails(
            id: stockKeepingUnitId,
            name: "Updated Widget",
            description: null,
            baseUnitOfMeasureId: baseUnitOfMeasureId,
            updatedAtUtc: DateTimeOffset.Parse("2026-06-10T01:00:00Z"));

        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson(details),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsCatalogApiClient apiClient = new(httpClient);

        ApiResult<StockKeepingUnitDetails> result = await apiClient.TryUpdateStockKeepingUnitDetailsAsync(
            stockKeepingUnitId,
            new UpdateStockKeepingUnitDetailsRequest(
                Name: "Updated Widget",
                Description: null,
                BaseUnitOfMeasureId: baseUnitOfMeasureId),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(stockKeepingUnitId, result.Value.Id);
        Assert.Equal(baseUnitOfMeasureId, result.Value.BaseUnitOfMeasureId);
        Assert.Equal(HttpMethod.Put, handler.RequestMethod);
        Assert.Equal($"/api/wms/catalog/skus/{stockKeepingUnitId}", handler.RequestPath);

        using JsonDocument requestBody = JsonDocument.Parse(handler.RequestBody);
        Assert.Equal(baseUnitOfMeasureId, requestBody.RootElement.GetProperty("baseUnitOfMeasureId").GetGuid());
    }

    [Fact]
    public async Task TryCreateUnitOfMeasureAsync_WhenSuccessful_PostsToUomRouteAndReturnsDetails()
    {
        UnitOfMeasureDetails details = CreateUnitOfMeasureDetails();

        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson(details),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsCatalogApiClient apiClient = new(httpClient);

        ApiResult<UnitOfMeasureDetails> result = await apiClient.TryCreateUnitOfMeasureAsync(
            new CreateUnitOfMeasureRequest(Code: "EA", Name: "Each", Symbol: "ea"),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(details.Id, result.Value.Id);
        Assert.Equal("EA", result.Value.Code);
        Assert.Equal(HttpMethod.Post, handler.RequestMethod);
        Assert.Equal("/api/wms/catalog/uoms", handler.RequestPath);

        using JsonDocument requestBody = JsonDocument.Parse(handler.RequestBody);
        JsonElement root = requestBody.RootElement;
        Assert.Equal("EA", root.GetProperty("code").GetString());
        Assert.Equal("Each", root.GetProperty("name").GetString());
        Assert.Equal("ea", root.GetProperty("symbol").GetString());
    }

    [Fact]
    public async Task ListUnitsOfMeasureAsync_WhenSuccessful_GetsFromUomRouteAndReturnsDetails()
    {
        UnitOfMeasureDetails details = CreateUnitOfMeasureDetails();

        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson(new ListResult<UnitOfMeasureDetails>(
                [details],
                TotalCount: 1,
                Skip: 0,
                Take: 20)),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsCatalogApiClient apiClient = new(httpClient);

        ListResult<UnitOfMeasureDetails> result = await apiClient.ListUnitsOfMeasureAsync(
            new ListUnitsOfMeasureRequest
            {
                SearchText = "EA",
                SortBy = UnitOfMeasureSortBy.Code,
                IncludeInactive = true
            },
            TestContext.Current.CancellationToken);

        UnitOfMeasureDetails item = Assert.Single(result.Items);
        Assert.Equal(details.Id, item.Id);
        Assert.Equal("EA", item.Code);
        Assert.Equal(HttpMethod.Get, handler.RequestMethod);
        Assert.Equal("/api/wms/catalog/uoms", handler.RequestPath);
        Assert.Equal("?searchText=EA&sortBy=Code&includeInactive=true", handler.RequestQuery);
    }

    [Fact]
    public async Task GetUnitOfMeasureByIdAsync_WhenSuccessful_GetsFromUomRouteAndReturnsDetails()
    {
        Guid unitOfMeasureId = Guid.Parse("018f0000-0000-7000-8000-000000000002");
        UnitOfMeasureDetails details = CreateUnitOfMeasureDetails(id: unitOfMeasureId);

        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson(details),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsCatalogApiClient apiClient = new(httpClient);

        UnitOfMeasureDetails result = await apiClient.GetUnitOfMeasureByIdAsync(
            unitOfMeasureId,
            TestContext.Current.CancellationToken);

        Assert.Equal(unitOfMeasureId, result.Id);
        Assert.Equal("EA", result.Code);
        Assert.Equal(HttpMethod.Get, handler.RequestMethod);
        Assert.Equal($"/api/wms/catalog/uoms/{unitOfMeasureId}", handler.RequestPath);
    }

    [Fact]
    public async Task TryUpdateUnitOfMeasureDetailsAsync_WhenSuccessful_PutsToUomRouteAndReturnsDetails()
    {
        Guid unitOfMeasureId = Guid.Parse("018f0000-0000-7000-8000-000000000002");
        UnitOfMeasureDetails details = CreateUnitOfMeasureDetails(
            id: unitOfMeasureId,
            name: "Each updated",
            symbol: "each",
            updatedAtUtc: DateTimeOffset.Parse("2026-06-10T00:00:00Z"));

        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson(details),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsCatalogApiClient apiClient = new(httpClient);

        ApiResult<UnitOfMeasureDetails> result = await apiClient.TryUpdateUnitOfMeasureDetailsAsync(
            unitOfMeasureId,
            new UpdateUnitOfMeasureDetailsRequest(Name: "Each updated", Symbol: "each"),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(unitOfMeasureId, result.Value.Id);
        Assert.Equal("Each updated", result.Value.Name);
        Assert.Equal(HttpMethod.Put, handler.RequestMethod);
        Assert.Equal($"/api/wms/catalog/uoms/{unitOfMeasureId}", handler.RequestPath);

        using JsonDocument requestBody = JsonDocument.Parse(handler.RequestBody);
        JsonElement root = requestBody.RootElement;
        Assert.Equal("Each updated", root.GetProperty("name").GetString());
        Assert.Equal("each", root.GetProperty("symbol").GetString());
    }

    [Fact]
    public async Task TryDeactivateUnitOfMeasureAsync_WhenSuccessful_PostsToUomDeactivateRouteAndReturnsDetails()
    {
        Guid unitOfMeasureId = Guid.Parse("018f0000-0000-7000-8000-000000000002");
        UnitOfMeasureDetails details = CreateUnitOfMeasureDetails(id: unitOfMeasureId, isActive: false);

        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson(details),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsCatalogApiClient apiClient = new(httpClient);

        ApiResult<UnitOfMeasureDetails> result = await apiClient.TryDeactivateUnitOfMeasureAsync(
            unitOfMeasureId,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.False(result.Value.IsActive);
        Assert.Equal(HttpMethod.Post, handler.RequestMethod);
        Assert.Equal($"/api/wms/catalog/uoms/{unitOfMeasureId}/deactivate", handler.RequestPath);
    }

    [Fact]
    public async Task TryReactivateUnitOfMeasureAsync_WhenSuccessful_PostsToUomReactivateRouteAndReturnsDetails()
    {
        Guid unitOfMeasureId = Guid.Parse("018f0000-0000-7000-8000-000000000002");
        UnitOfMeasureDetails details = CreateUnitOfMeasureDetails(id: unitOfMeasureId, isActive: true);

        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson(details),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsCatalogApiClient apiClient = new(httpClient);

        ApiResult<UnitOfMeasureDetails> result = await apiClient.TryReactivateUnitOfMeasureAsync(
            unitOfMeasureId,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.IsActive);
        Assert.Equal(HttpMethod.Post, handler.RequestMethod);
        Assert.Equal($"/api/wms/catalog/uoms/{unitOfMeasureId}/reactivate", handler.RequestPath);
    }

    [Fact]
    public async Task TryCreateStockKeepingUnitAsync_WhenMalformedErrorReturned_ReturnsFallbackFailureResult()
    {
        using StubHttpMessageHandler handler = new(
            HttpStatusCode.BadRequest,
            "not a valid problem details json",
            "application/problem+json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsCatalogApiClient apiClient = new(httpClient);

        ApiResult<StockKeepingUnitDetails> result = await apiClient.TryCreateStockKeepingUnitAsync(
            new CreateStockKeepingUnitRequest(
                Code: "ITEM-001",
                Name: "Widget",
                Description: null,
                BaseUnitOfMeasureId: Guid.NewGuid()),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(400, result.Error.Status);
        Assert.Equal(
            "API request failed for POST '/api/wms/catalog/skus'. Status code: 400 BadRequest.",
            result.Error.Message);
        Assert.Empty(result.Error.Extensions);
    }

    [Fact]
    public async Task TryCreateSkuBarcodeAsync_WhenSuccessful_PostsToSkuBarcodeRouteAndReturnsDetails()
    {
        Guid stockKeepingUnitId = Guid.Parse("018f0000-0000-7000-8000-000000000001");
        SkuBarcodeDetails details = CreateSkuBarcodeDetails(stockKeepingUnitId: stockKeepingUnitId);

        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson(details),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsCatalogApiClient apiClient = new(httpClient);

        ApiResult<SkuBarcodeDetails> result = await apiClient.TryCreateSkuBarcodeAsync(
            new CreateSkuBarcodeRequest(
                StockKeepingUnitId: stockKeepingUnitId,
                Value: "  AbC-123  ",
                Symbology: "Code128",
                IsPrimary: true),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(details.Id, result.Value.Id);
        Assert.Equal(stockKeepingUnitId, result.Value.StockKeepingUnitId);
        Assert.Equal("AbC-123", result.Value.Value);
        Assert.True(result.Value.IsPrimary);
        Assert.Equal(HttpMethod.Post, handler.RequestMethod);
        Assert.Equal("/api/wms/catalog/sku-barcodes", handler.RequestPath);

        using JsonDocument requestBody = JsonDocument.Parse(handler.RequestBody);
        JsonElement root = requestBody.RootElement;
        Assert.Equal(stockKeepingUnitId, root.GetProperty("stockKeepingUnitId").GetGuid());
        Assert.Equal("  AbC-123  ", root.GetProperty("value").GetString());
        Assert.Equal("Code128", root.GetProperty("symbology").GetString());
        Assert.True(root.GetProperty("isPrimary").GetBoolean());
    }

    [Fact]
    public async Task ListSkuBarcodesAsync_WhenSuccessful_GetsFromSkuBarcodeRouteAndReturnsDetails()
    {
        Guid stockKeepingUnitId = Guid.Parse("018f0000-0000-7000-8000-000000000001");
        SkuBarcodeDetails details = CreateSkuBarcodeDetails(stockKeepingUnitId: stockKeepingUnitId);

        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson(new ListResult<SkuBarcodeDetails>(
                [details],
                TotalCount: 1,
                Skip: 5,
                Take: 10)),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsCatalogApiClient apiClient = new(httpClient);

        ListResult<SkuBarcodeDetails> result = await apiClient.ListSkuBarcodesAsync(
            new ListSkuBarcodesRequest(
                Skip: 5,
                Take: 10,
                SearchText: "AbC",
                SortBy: "value",
                SortDescending: true,
                IncludeInactive: true,
                StockKeepingUnitId: stockKeepingUnitId),
            TestContext.Current.CancellationToken);

        SkuBarcodeDetails item = Assert.Single(result.Items);
        Assert.Equal(details.Id, item.Id);
        Assert.Equal(stockKeepingUnitId, item.StockKeepingUnitId);
        Assert.Equal(5, result.Skip);
        Assert.Equal(10, result.Take);
        Assert.Equal(HttpMethod.Get, handler.RequestMethod);
        Assert.Equal("/api/wms/catalog/sku-barcodes", handler.RequestPath);
        Assert.Equal(
            $"?skip=5&take=10&searchText=AbC&sortBy=value&sortDescending=true&includeInactive=true&stockKeepingUnitId={stockKeepingUnitId}",
            handler.RequestQuery);
    }

    [Fact]
    public async Task GetSkuBarcodeByIdAsync_WhenSuccessful_GetsFromSkuBarcodeRouteAndReturnsDetails()
    {
        Guid skuBarcodeId = Guid.Parse("018f0000-0000-7000-8000-000000000042");
        SkuBarcodeDetails details = CreateSkuBarcodeDetails(id: skuBarcodeId, isPrimary: false, isActive: false);

        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson(details),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsCatalogApiClient apiClient = new(httpClient);

        SkuBarcodeDetails result = await apiClient.GetSkuBarcodeByIdAsync(
            skuBarcodeId,
            TestContext.Current.CancellationToken);

        Assert.Equal(skuBarcodeId, result.Id);
        Assert.False(result.IsActive);
        Assert.Equal(HttpMethod.Get, handler.RequestMethod);
        Assert.Equal($"/api/wms/catalog/sku-barcodes/{skuBarcodeId}", handler.RequestPath);
    }

    [Fact]
    public async Task TryUpdateSkuBarcodeDetailsAsync_WhenSuccessful_PutsToSkuBarcodeRouteAndReturnsDetails()
    {
        Guid skuBarcodeId = Guid.Parse("018f0000-0000-7000-8000-000000000042");
        SkuBarcodeDetails details = CreateSkuBarcodeDetails(
            id: skuBarcodeId,
            value: "AbC-789",
            symbology: "QrCode",
            isPrimary: true,
            updatedAtUtc: DateTimeOffset.Parse("2026-06-10T00:00:00Z"));

        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson(details),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsCatalogApiClient apiClient = new(httpClient);

        ApiResult<SkuBarcodeDetails> result = await apiClient.TryUpdateSkuBarcodeDetailsAsync(
            skuBarcodeId,
            new UpdateSkuBarcodeDetailsRequest(
                Value: "  AbC-789  ",
                Symbology: "QrCode",
                IsPrimary: true),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(skuBarcodeId, result.Value.Id);
        Assert.Equal("AbC-789", result.Value.Value);
        Assert.Equal("QrCode", result.Value.Symbology);
        Assert.Equal(HttpMethod.Put, handler.RequestMethod);
        Assert.Equal($"/api/wms/catalog/sku-barcodes/{skuBarcodeId}", handler.RequestPath);

        using JsonDocument requestBody = JsonDocument.Parse(handler.RequestBody);
        JsonElement root = requestBody.RootElement;
        Assert.Equal("  AbC-789  ", root.GetProperty("value").GetString());
        Assert.Equal("QrCode", root.GetProperty("symbology").GetString());
        Assert.True(root.GetProperty("isPrimary").GetBoolean());
    }

    [Fact]
    public async Task TryDeactivateSkuBarcodeAsync_WhenSuccessful_PostsToDeactivateRouteAndReturnsDetails()
    {
        Guid skuBarcodeId = Guid.Parse("018f0000-0000-7000-8000-000000000042");
        SkuBarcodeDetails details = CreateSkuBarcodeDetails(id: skuBarcodeId, isPrimary: false, isActive: false);

        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson(details),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsCatalogApiClient apiClient = new(httpClient);

        ApiResult<SkuBarcodeDetails> result = await apiClient.TryDeactivateSkuBarcodeAsync(
            skuBarcodeId,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.False(result.Value.IsPrimary);
        Assert.False(result.Value.IsActive);
        Assert.Equal(HttpMethod.Post, handler.RequestMethod);
        Assert.Equal($"/api/wms/catalog/sku-barcodes/{skuBarcodeId}/deactivate", handler.RequestPath);
    }

    [Fact]
    public async Task TryReactivateSkuBarcodeAsync_WhenSuccessful_PostsToReactivateRouteAndReturnsDetails()
    {
        Guid skuBarcodeId = Guid.Parse("018f0000-0000-7000-8000-000000000042");
        SkuBarcodeDetails details = CreateSkuBarcodeDetails(id: skuBarcodeId, isPrimary: false, isActive: true);

        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson(details),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsCatalogApiClient apiClient = new(httpClient);

        ApiResult<SkuBarcodeDetails> result = await apiClient.TryReactivateSkuBarcodeAsync(
            skuBarcodeId,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.False(result.Value.IsPrimary);
        Assert.True(result.Value.IsActive);
        Assert.Equal(HttpMethod.Post, handler.RequestMethod);
        Assert.Equal($"/api/wms/catalog/sku-barcodes/{skuBarcodeId}/reactivate", handler.RequestPath);
    }

    private static HttpClient CreateHttpClient(StubHttpMessageHandler handler)
    {
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://myrmex.test")
        };
    }

    private static string SerializeJson<T>(T value)
    {
        return JsonSerializer.Serialize(value, JsonOptions);
    }

    private static StockKeepingUnitDetails CreateStockKeepingUnitDetails(
        Guid? id = null,
        string code = "ITEM-001",
        string name = "Widget",
        string? description = "Sellable widget",
        Guid? baseUnitOfMeasureId = null,
        bool isActive = true,
        DateTimeOffset? updatedAtUtc = null)
    {
        return new StockKeepingUnitDetails(
            id ?? Guid.Parse("018f0000-0000-7000-8000-000000000001"),
            code,
            name,
            description,
            baseUnitOfMeasureId ?? Guid.Parse("018f0000-0000-7000-8000-000000000111"),
            isActive,
            DateTimeOffset.Parse("2026-06-10T00:00:00Z"),
            updatedAtUtc);
    }

    private static UnitOfMeasureDetails CreateUnitOfMeasureDetails(
        Guid? id = null,
        string code = "EA",
        string name = "Each",
        string? symbol = "ea",
        bool isActive = true,
        DateTimeOffset? updatedAtUtc = null)
    {
        return new UnitOfMeasureDetails(
            id ?? Guid.Parse("018f0000-0000-7000-8000-000000000002"),
            code,
            name,
            symbol,
            isActive,
            DateTimeOffset.Parse("2026-06-09T00:00:00Z"),
            updatedAtUtc);
    }

    private static SkuBarcodeDetails CreateSkuBarcodeDetails(
        Guid? id = null,
        Guid? stockKeepingUnitId = null,
        string value = "AbC-123",
        string symbology = "Code128",
        bool isPrimary = true,
        bool isActive = true,
        DateTimeOffset? updatedAtUtc = null)
    {
        return new SkuBarcodeDetails(
            id ?? Guid.Parse("018f0000-0000-7000-8000-000000000042"),
            stockKeepingUnitId ?? Guid.Parse("018f0000-0000-7000-8000-000000000001"),
            value,
            symbology,
            isPrimary,
            isActive,
            DateTimeOffset.Parse("2026-06-09T00:00:00Z"),
            updatedAtUtc);
    }

    private sealed class StubHttpMessageHandler(
        HttpStatusCode statusCode,
        string content,
        string mediaType) : HttpMessageHandler
    {
        public HttpMethod? RequestMethod { get; private set; }

        public string? RequestPath { get; private set; }

        public string? RequestQuery { get; private set; }

        public string RequestBody { get; private set; } = string.Empty;

        public CancellationToken RequestCancellationToken { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestMethod = request.Method;
            RequestPath = request.RequestUri?.AbsolutePath;
            RequestQuery = request.RequestUri?.Query;
            RequestCancellationToken = cancellationToken;

            if (request.Content is not null)
            {
                RequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(
                    content,
                    Encoding.UTF8,
                    mediaType)
            };
        }
    }
}
