using Myrmex.WebApp.Wms.Api;
using Myrmex.WebApp.Wms.Inventory;
using System.Text;
using System.Text.Json;

namespace Myrmex.Tests.Wms.Inventory.Client;

public sealed class WmsInventoryApiClientTests
{
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
        Assert.Equal(stockKeepingUnitId, details.StockKeepingUnitId);
        Assert.Equal("ITEM-001", details.StockKeepingUnitCode);
        Assert.Equal(storageLocationId, details.StorageLocationId);
        Assert.Equal("A-01-01", details.StorageLocationCode);
        Assert.Equal(warehouseId, details.WarehouseId);
        Assert.Equal("MAIN", details.WarehouseCode);
        Assert.Equal(baseUnitOfMeasureId, details.BaseUnitOfMeasureId);
        Assert.Equal("EA", details.BaseUnitOfMeasureCode);
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

    private static HttpClient CreateHttpClient(StubHttpMessageHandler handler)
    {
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://myrmex.test")
        };
    }

    private sealed class StubHttpMessageHandler(
        HttpStatusCode statusCode,
        string content,
        string mediaType) : HttpMessageHandler
    {
        public HttpMethod? RequestMethod { get; private set; }

        public string? RequestPath { get; private set; }

        public string RequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestMethod = request.Method;
            RequestPath = request.RequestUri?.AbsolutePath;

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
