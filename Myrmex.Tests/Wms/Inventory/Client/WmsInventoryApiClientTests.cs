using Myrmex.Shared.Wms.Inventory;
using Myrmex.WebApp.Wms.Api;
using Myrmex.WebApp.Wms.Inventory;
using System.Text;
using System.Text.Json;
using System.Web;

namespace Myrmex.Tests.Wms.Inventory.Client;

public sealed class WmsInventoryApiClientTests
{
    [Fact]
    public async Task ListInventoryBalancesAsync_WhenSuccessful_BuildsQueryStringAndParsesResponse()
    {
        Guid inventoryBalanceId = Guid.Parse("018f0000-0000-7000-8000-000000000401");
        Guid stockKeepingUnitId = Guid.Parse("018f0000-0000-7000-8000-000000000201");
        Guid storageLocationId = Guid.Parse("018f0000-0000-7000-8000-000000000301");
        Guid warehouseId = Guid.Parse("018f0000-0000-7000-8000-000000000101");
        Guid baseUnitOfMeasureId = Guid.Parse("018f0000-0000-7000-8000-000000000111");

        string responseJson = $$"""
            {
              "items": [
                {
                  "id": "{{inventoryBalanceId}}",
                  "stockKeepingUnitId": "{{stockKeepingUnitId}}",
                  "stockKeepingUnitCode": "ITEM-001",
                  "stockKeepingUnitName": "Widget",
                  "storageLocationId": "{{storageLocationId}}",
                  "storageLocationCode": "A-01-01",
                  "storageLocationName": "A-01-01",
                  "warehouseId": "{{warehouseId}}",
                  "warehouseCode": "MAIN",
                  "warehouseName": "Main Warehouse",
                  "baseUnitOfMeasureId": "{{baseUnitOfMeasureId}}",
                  "baseUnitOfMeasureCode": "EA",
                  "baseUnitOfMeasureSymbol": "ea",
                  "quantity": 10.0,
                  "createdAtUtc": "2026-06-11T00:00:00+00:00",
                  "updatedAtUtc": null
                }
              ],
              "totalCount": 1,
              "skip": 5,
              "take": 10
            }
            """;

        StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            responseJson,
            "application/json");

        using HttpClient httpClient = CreateHttpClient(handler);
        WmsInventoryApiClient apiClient = new(httpClient);

        ListResult<InventoryBalanceDetails> result = await apiClient.ListInventoryBalancesAsync(
            new ListInventoryBalancesRequest(
                Skip: 5,
                Take: 10,
                SortBy: "quantity",
                SortDescending: true,
                StockKeepingUnitId: stockKeepingUnitId,
                StorageLocationId: storageLocationId,
                WarehouseId: warehouseId),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Get, handler.RequestMethod);
        Assert.Equal("/api/wms/inventory/balances", handler.RequestPath);

        Dictionary<string, string> query = ParseQuery(handler.RequestQuery);
        Assert.Equal("5", query["skip"]);
        Assert.Equal("10", query["take"]);
        Assert.Equal("quantity", query["sortBy"]);
        Assert.Equal("true", query["sortDescending"]);
        Assert.Equal(stockKeepingUnitId.ToString(), query["stockKeepingUnitId"]);
        Assert.Equal(storageLocationId.ToString(), query["storageLocationId"]);
        Assert.Equal(warehouseId.ToString(), query["warehouseId"]);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(5, result.Skip);
        Assert.Equal(10, result.Take);

        InventoryBalanceDetails details = Assert.Single(result.Items);
        Assert.Equal(inventoryBalanceId, details.Id);
        Assert.Equal(stockKeepingUnitId, details.Sku.Id);
        Assert.Equal("ITEM-001", details.Sku.Code);
        Assert.Equal(storageLocationId, details.StorageLocation.Id);
        Assert.Equal("A-01-01", details.StorageLocation.Code);
        Assert.Equal(warehouseId, details.StorageLocation.Warehouse.Id);
        Assert.Equal("MAIN", details.StorageLocation.Warehouse.Code);
        Assert.Equal(baseUnitOfMeasureId, details.Sku.BaseUom.Id);
        Assert.Equal("EA", details.Sku.BaseUom.Code);
        Assert.Equal(10, details.Quantity);
    }

    [Fact]
    public async Task ListInventoryBalancesAsync_WhenOptionalFiltersAreNotProvided_OmitsFilterQueryParameters()
    {
        const string responseJson = """
            {
              "items": [],
              "totalCount": 0,
              "skip": 0,
              "take": 20
            }
            """;

        StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            responseJson,
            "application/json");

        using HttpClient httpClient = CreateHttpClient(handler);
        WmsInventoryApiClient apiClient = new(httpClient);

        ListResult<InventoryBalanceDetails> result = await apiClient.ListInventoryBalancesAsync(
            new ListInventoryBalancesRequest(),
            TestContext.Current.CancellationToken);

        Dictionary<string, string> query = ParseQuery(handler.RequestQuery);
        Assert.Equal("0", query["skip"]);
        Assert.Equal("20", query["take"]);
        Assert.Equal("false", query["sortDescending"]);
        Assert.False(query.ContainsKey("sortBy"));
        Assert.False(query.ContainsKey("stockKeepingUnitId"));
        Assert.False(query.ContainsKey("storageLocationId"));
        Assert.False(query.ContainsKey("warehouseId"));
        Assert.False(query.ContainsKey("includeInactive"));
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task ListInventoryBalancesAsync_WhenProblemDetailsReturned_ThrowsApiException()
    {
        const string problemJson = """
            {
              "type": "https://httpstatuses.com/500",
              "title": "Internal Server Error",
              "status": 500,
              "detail": "Inventory balances could not be loaded.",
              "code": "InventoryBalance.ListFailed"
            }
            """;

        using HttpClient httpClient = CreateHttpClient(new StubHttpMessageHandler(
            HttpStatusCode.InternalServerError,
            problemJson,
            "application/problem+json"));
        WmsInventoryApiClient apiClient = new(httpClient);

        ApiException exception = await Assert.ThrowsAsync<ApiException>(() =>
            apiClient.ListInventoryBalancesAsync(
                new ListInventoryBalancesRequest(),
                TestContext.Current.CancellationToken));

        Assert.Equal(500, exception.Status);
        Assert.Equal("Inventory balances could not be loaded.", exception.Message);
        Assert.Equal("InventoryBalance.ListFailed", exception.Extensions["code"]);
    }

    [Fact]
    public async Task ListInventoryBalancesAsync_WhenMalformedErrorReturned_ThrowsApiExceptionWithFallbackMessage()
    {
        using HttpClient httpClient = CreateHttpClient(new StubHttpMessageHandler(
            HttpStatusCode.BadRequest,
            "not valid problem details",
            "application/problem+json"));
        WmsInventoryApiClient apiClient = new(httpClient);

        ApiException exception = await Assert.ThrowsAsync<ApiException>(() =>
            apiClient.ListInventoryBalancesAsync(
                new ListInventoryBalancesRequest(),
                TestContext.Current.CancellationToken));

        Assert.Equal(400, exception.Status);
        Assert.Equal(
            "API request failed for GET '/api/wms/inventory/balances?skip=0&take=20&sortDescending=false'. Status code: 400 BadRequest.",
            exception.Message);
        Assert.Empty(exception.Extensions);
    }

    [Fact]
    public async Task TryCreateInventoryBalanceAsync_WhenSuccessful_PostsRequestAndParsesResponse()
    {
        Guid inventoryBalanceId = Guid.Parse("018f0000-0000-7000-8000-000000000401");
        Guid stockKeepingUnitId = Guid.Parse("018f0000-0000-7000-8000-000000000201");
        Guid storageLocationId = Guid.Parse("018f0000-0000-7000-8000-000000000301");
        Guid warehouseId = Guid.Parse("018f0000-0000-7000-8000-000000000101");
        Guid baseUnitOfMeasureId = Guid.Parse("018f0000-0000-7000-8000-000000000111");

        string responseJson = $$"""
            {
              "id": "{{inventoryBalanceId}}",
              "stockKeepingUnitId": "{{stockKeepingUnitId}}",
              "stockKeepingUnitCode": "ITEM-001",
              "stockKeepingUnitName": "Widget",
              "storageLocationId": "{{storageLocationId}}",
              "storageLocationCode": "A-01-01",
              "storageLocationName": "A-01-01",
              "warehouseId": "{{warehouseId}}",
              "warehouseCode": "MAIN",
              "warehouseName": "Main Warehouse",
              "baseUnitOfMeasureId": "{{baseUnitOfMeasureId}}",
              "baseUnitOfMeasureCode": "EA",
              "baseUnitOfMeasureSymbol": "ea",
              "quantity": 10.0,
              "createdAtUtc": "2026-06-11T00:00:00+00:00",
              "updatedAtUtc": null
            }
            """;

        StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            responseJson,
            "application/json");

        using HttpClient httpClient = CreateHttpClient(handler);
        WmsInventoryApiClient apiClient = new(httpClient);

        CreateInventoryBalanceRequest request = new(
            stockKeepingUnitId,
            storageLocationId,
            Quantity: 10);

        ApiResult<InventoryBalanceDetails> result = await apiClient.TryCreateInventoryBalanceAsync(
            request,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(HttpMethod.Post, handler.RequestMethod);
        Assert.Equal("/api/wms/inventory/balances", handler.RequestPath);

        using JsonDocument requestJson = JsonDocument.Parse(handler.RequestBody);
        Assert.Equal(stockKeepingUnitId, requestJson.RootElement.GetProperty("stockKeepingUnitId").GetGuid());
        Assert.Equal(storageLocationId, requestJson.RootElement.GetProperty("storageLocationId").GetGuid());
        Assert.Equal(10, requestJson.RootElement.GetProperty("quantity").GetDecimal());

        InventoryBalanceDetails details = result.Value;

        Assert.Equal(inventoryBalanceId, details.Id);
        Assert.Equal(stockKeepingUnitId, details.Sku.Id);
        Assert.Equal("ITEM-001", details.Sku.Code);
        Assert.Equal(storageLocationId, details.StorageLocation.Id);
        Assert.Equal("A-01-01", details.StorageLocation.Code);
        Assert.Equal(warehouseId, details.StorageLocation.Warehouse.Id);
        Assert.Equal("MAIN", details.StorageLocation.Warehouse.Code);
        Assert.Equal(baseUnitOfMeasureId, details.Sku.BaseUom.Id);
        Assert.Equal("EA", details.Sku.BaseUom.Code);
        Assert.Equal(10, details.Quantity);
        Assert.Null(details.UpdatedAtUtc);
    }

    [Fact]
    public async Task TryCreateInventoryBalanceAsync_WhenValidationProblemReturned_ReturnsFailureResult()
    {
        const string problemJson = """
            {
              "type": "https://httpstatuses.com/400",
              "title": "Bad Request",
              "status": 400,
              "detail": "One or more validation errors occurred.",
              "code": "Validation.Invalid",
              "field": "quantity"
            }
            """;

        using HttpClient httpClient = CreateHttpClient(new StubHttpMessageHandler(
            HttpStatusCode.BadRequest,
            problemJson,
            "application/problem+json"));
        WmsInventoryApiClient apiClient = new(httpClient);

        ApiResult<InventoryBalanceDetails> result = await apiClient.TryCreateInventoryBalanceAsync(
            new CreateInventoryBalanceRequest(Guid.NewGuid(), Guid.NewGuid(), Quantity: -1),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(400, result.Error.Status);
        Assert.Equal("One or more validation errors occurred.", result.Error.Message);
        Assert.Equal("Validation.Invalid", result.Error.Extensions["code"]);
        Assert.Equal("quantity", result.Error.Extensions["field"]);
    }

    [Fact]
    public async Task TryCreateInventoryBalanceAsync_WhenMissingReferenceReturned_ReturnsFailureResult()
    {
        const string problemJson = """
            {
              "type": "https://httpstatuses.com/404",
              "title": "Not Found",
              "status": 404,
              "detail": "SKU was not found for inventory balance creation.",
              "code": "InventoryBalance.StockKeepingUnitNotFound",
              "field": "stockKeepingUnitId"
            }
            """;

        using HttpClient httpClient = CreateHttpClient(new StubHttpMessageHandler(
            HttpStatusCode.NotFound,
            problemJson,
            "application/problem+json"));
        WmsInventoryApiClient apiClient = new(httpClient);

        ApiResult<InventoryBalanceDetails> result = await apiClient.TryCreateInventoryBalanceAsync(
            new CreateInventoryBalanceRequest(Guid.NewGuid(), Guid.NewGuid(), Quantity: 10),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(404, result.Error.Status);
        Assert.Equal("InventoryBalance.StockKeepingUnitNotFound", result.Error.Extensions["code"]);
        Assert.Equal("stockKeepingUnitId", result.Error.Extensions["field"]);
    }

    [Fact]
    public async Task TryCreateInventoryBalanceAsync_WhenDuplicateReturned_ReturnsFailureResult()
    {
        const string problemJson = """
            {
              "type": "https://httpstatuses.com/409",
              "title": "Conflict",
              "status": 409,
              "detail": "Inventory balance for the same SKU and storage location already exists.",
              "code": "InventoryBalance.DuplicateStockKeepingUnitStorageLocation",
              "field": "stockKeepingUnitId"
            }
            """;

        using HttpClient httpClient = CreateHttpClient(new StubHttpMessageHandler(
            HttpStatusCode.Conflict,
            problemJson,
            "application/problem+json"));
        WmsInventoryApiClient apiClient = new(httpClient);

        ApiResult<InventoryBalanceDetails> result = await apiClient.TryCreateInventoryBalanceAsync(
            new CreateInventoryBalanceRequest(Guid.NewGuid(), Guid.NewGuid(), Quantity: 10),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(409, result.Error.Status);
        Assert.Equal("InventoryBalance.DuplicateStockKeepingUnitStorageLocation", result.Error.Extensions["code"]);
        Assert.Equal("stockKeepingUnitId", result.Error.Extensions["field"]);
    }

    [Fact]
    public async Task TryUpdateInventoryBalanceQuantityAsync_WhenSuccessful_PutsQuantityOnlyPayloadAndParsesResponse()
    {
        Guid inventoryBalanceId = Guid.Parse("018f0000-0000-7000-8000-000000000401");
        Guid stockKeepingUnitId = Guid.Parse("018f0000-0000-7000-8000-000000000201");
        Guid storageLocationId = Guid.Parse("018f0000-0000-7000-8000-000000000301");
        Guid warehouseId = Guid.Parse("018f0000-0000-7000-8000-000000000101");
        Guid baseUnitOfMeasureId = Guid.Parse("018f0000-0000-7000-8000-000000000111");

        string responseJson = $$"""
            {
              "id": "{{inventoryBalanceId}}",
              "stockKeepingUnitId": "{{stockKeepingUnitId}}",
              "stockKeepingUnitCode": "ITEM-001",
              "stockKeepingUnitName": "Widget",
              "storageLocationId": "{{storageLocationId}}",
              "storageLocationCode": "A-01-01",
              "storageLocationName": "A-01-01",
              "warehouseId": "{{warehouseId}}",
              "warehouseCode": "MAIN",
              "warehouseName": "Main Warehouse",
              "baseUnitOfMeasureId": "{{baseUnitOfMeasureId}}",
              "baseUnitOfMeasureCode": "EA",
              "baseUnitOfMeasureSymbol": "ea",
              "quantity": 5.0,
              "createdAtUtc": "2026-06-11T00:00:00+00:00",
              "updatedAtUtc": "2026-06-11T01:00:00+00:00"
            }
            """;

        StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            responseJson,
            "application/json");

        using HttpClient httpClient = CreateHttpClient(handler);
        WmsInventoryApiClient apiClient = new(httpClient);

        ApiResult<InventoryBalanceDetails> result = await apiClient.TryUpdateInventoryBalanceQuantityAsync(
            inventoryBalanceId,
            new UpdateInventoryBalanceQuantityRequest(Quantity: 5),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(HttpMethod.Put, handler.RequestMethod);
        Assert.Equal($"/api/wms/inventory/balances/{inventoryBalanceId}/quantity", handler.RequestPath);

        using JsonDocument requestJson = JsonDocument.Parse(handler.RequestBody);
        Assert.Equal(5, requestJson.RootElement.GetProperty("quantity").GetDecimal());
        Assert.False(requestJson.RootElement.TryGetProperty("stockKeepingUnitId", out _));
        Assert.False(requestJson.RootElement.TryGetProperty("storageLocationId", out _));

        InventoryBalanceDetails details = result.Value;
        Assert.Equal(inventoryBalanceId, details.Id);
        Assert.Equal(stockKeepingUnitId, details.Sku.Id);
        Assert.Equal(storageLocationId, details.StorageLocation.Id);
        Assert.Equal(warehouseId, details.StorageLocation.Warehouse.Id);
        Assert.Equal(baseUnitOfMeasureId, details.Sku.BaseUom.Id);
        Assert.Equal(5, details.Quantity);
        Assert.NotNull(details.UpdatedAtUtc);
    }

    [Fact]
    public async Task TryUpdateInventoryBalanceQuantityAsync_WhenValidationProblemReturned_ReturnsFailureResult()
    {
        const string problemJson = """
            {
              "type": "https://httpstatuses.com/400",
              "title": "Bad Request",
              "status": 400,
              "detail": "One or more validation errors occurred.",
              "code": "Validation.Invalid",
              "field": "quantity"
            }
            """;

        using HttpClient httpClient = CreateHttpClient(new StubHttpMessageHandler(
            HttpStatusCode.BadRequest,
            problemJson,
            "application/problem+json"));
        WmsInventoryApiClient apiClient = new(httpClient);

        ApiResult<InventoryBalanceDetails> result = await apiClient.TryUpdateInventoryBalanceQuantityAsync(
            Guid.NewGuid(),
            new UpdateInventoryBalanceQuantityRequest(Quantity: -1),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(400, result.Error.Status);
        Assert.Equal("One or more validation errors occurred.", result.Error.Message);
        Assert.Equal("Validation.Invalid", result.Error.Extensions["code"]);
        Assert.Equal("quantity", result.Error.Extensions["field"]);
    }

    [Fact]
    public async Task TryUpdateInventoryBalanceQuantityAsync_WhenNotFoundReturned_ReturnsFailureResult()
    {
        const string problemJson = """
            {
              "type": "https://httpstatuses.com/404",
              "title": "Not Found",
              "status": 404,
              "detail": "Inventory balance was not found.",
              "code": "InventoryBalance.NotFound"
            }
            """;

        using HttpClient httpClient = CreateHttpClient(new StubHttpMessageHandler(
            HttpStatusCode.NotFound,
            problemJson,
            "application/problem+json"));
        WmsInventoryApiClient apiClient = new(httpClient);

        ApiResult<InventoryBalanceDetails> result = await apiClient.TryUpdateInventoryBalanceQuantityAsync(
            Guid.NewGuid(),
            new UpdateInventoryBalanceQuantityRequest(Quantity: 5),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(404, result.Error.Status);
        Assert.Equal("Inventory balance was not found.", result.Error.Message);
        Assert.Equal("InventoryBalance.NotFound", result.Error.Extensions["code"]);
    }

    [Fact]
    public async Task GetInventoryBalanceByIdAsync_WhenSuccessful_ParsesResponse()
    {
        Guid inventoryBalanceId = Guid.Parse("018f0000-0000-7000-8000-000000000401");
        Guid stockKeepingUnitId = Guid.Parse("018f0000-0000-7000-8000-000000000201");
        Guid storageLocationId = Guid.Parse("018f0000-0000-7000-8000-000000000301");
        Guid warehouseId = Guid.Parse("018f0000-0000-7000-8000-000000000101");
        Guid baseUnitOfMeasureId = Guid.Parse("018f0000-0000-7000-8000-000000000111");

        string responseJson = $$"""
            {
              "id": "{{inventoryBalanceId}}",
              "stockKeepingUnitId": "{{stockKeepingUnitId}}",
              "stockKeepingUnitCode": "ITEM-001",
              "stockKeepingUnitName": "Widget",
              "storageLocationId": "{{storageLocationId}}",
              "storageLocationCode": "A-01-01",
              "storageLocationName": "A-01-01",
              "warehouseId": "{{warehouseId}}",
              "warehouseCode": "MAIN",
              "warehouseName": "Main Warehouse",
              "baseUnitOfMeasureId": "{{baseUnitOfMeasureId}}",
              "baseUnitOfMeasureCode": "EA",
              "baseUnitOfMeasureSymbol": "ea",
              "quantity": 0.0,
              "createdAtUtc": "2026-06-11T00:00:00+00:00",
              "updatedAtUtc": "2026-06-11T01:00:00+00:00"
            }
            """;

        StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            responseJson,
            "application/json");

        using HttpClient httpClient = CreateHttpClient(handler);
        WmsInventoryApiClient apiClient = new(httpClient);

        InventoryBalanceDetails details = await apiClient.GetInventoryBalanceByIdAsync(
            inventoryBalanceId,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Get, handler.RequestMethod);
        Assert.Equal($"/api/wms/inventory/balances/{inventoryBalanceId}", handler.RequestPath);
        Assert.Equal(inventoryBalanceId, details.Id);
        Assert.Equal(stockKeepingUnitId, details.Sku.Id);
        Assert.Equal("ITEM-001", details.Sku.Code);
        Assert.Equal(storageLocationId, details.StorageLocation.Id);
        Assert.Equal("A-01-01", details.StorageLocation.Code);
        Assert.Equal(warehouseId, details.StorageLocation.Warehouse.Id);
        Assert.Equal(baseUnitOfMeasureId, details.Sku.BaseUom.Id);
        Assert.Equal(0, details.Quantity);
        Assert.NotNull(details.UpdatedAtUtc);
    }

    [Fact]
    public async Task GetInventoryBalanceByIdAsync_WhenProblemDetailsReturned_ThrowsApiException()
    {
        Guid inventoryBalanceId = Guid.Parse("018f0000-0000-7000-8000-000000000999");

        const string problemJson = """
            {
              "type": "https://httpstatuses.com/404",
              "title": "Not Found",
              "status": 404,
              "detail": "Inventory balance was not found.",
              "code": "InventoryBalance.NotFound"
            }
            """;

        using HttpClient httpClient = CreateHttpClient(new StubHttpMessageHandler(
            HttpStatusCode.NotFound,
            problemJson,
            "application/problem+json"));
        WmsInventoryApiClient apiClient = new(httpClient);

        ApiException exception = await Assert.ThrowsAsync<ApiException>(() =>
            apiClient.GetInventoryBalanceByIdAsync(
                inventoryBalanceId,
                TestContext.Current.CancellationToken));

        Assert.Equal(404, exception.Status);
        Assert.Equal("Inventory balance was not found.", exception.Message);
        Assert.Equal("InventoryBalance.NotFound", exception.Extensions["code"]);
    }

    [Fact]
    public async Task GetInventoryBalanceByIdAsync_WhenMalformedErrorReturned_ThrowsApiExceptionWithFallbackMessage()
    {
        Guid inventoryBalanceId = Guid.Parse("018f0000-0000-7000-8000-000000000999");

        using HttpClient httpClient = CreateHttpClient(new StubHttpMessageHandler(
            HttpStatusCode.BadRequest,
            "not valid problem details",
            "application/problem+json"));
        WmsInventoryApiClient apiClient = new(httpClient);

        ApiException exception = await Assert.ThrowsAsync<ApiException>(() =>
            apiClient.GetInventoryBalanceByIdAsync(
                inventoryBalanceId,
                TestContext.Current.CancellationToken));

        Assert.Equal(400, exception.Status);
        Assert.Equal(
            $"API request failed for GET '/api/wms/inventory/balances/{inventoryBalanceId}'. Status code: 400 BadRequest.",
            exception.Message);
        Assert.Empty(exception.Extensions);
    }

    private static HttpClient CreateHttpClient(StubHttpMessageHandler handler)
    {
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://myrmex.test")
        };
    }

    private static Dictionary<string, string> ParseQuery(string? query)
    {
        var queryString = HttpUtility.ParseQueryString(query ?? string.Empty);

        return queryString
            .AllKeys
            .Where(x => x is not null)
            .ToDictionary(x => x!, x => queryString[x!]!);
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

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestMethod = request.Method;
            RequestPath = request.RequestUri?.AbsolutePath;
            RequestQuery = request.RequestUri?.Query;

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
