using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Inventory;
using Myrmex.WebApp.Wms.Api;
using Myrmex.WebApp.Wms.Inventory;
using System.Text;
using System.Text.Json;
using System.Web;

namespace Myrmex.Tests.Wms.Inventory.Client;

public sealed class WmsInventoryApiClientTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task ListInventoryBalancesAsync_WhenRequestHasNoValues_OmitsNullableQueryParametersAndMapsNestedDetails()
    {
        InventoryBalanceDetails details = CreateInventoryBalanceDetails();
        ListResult<InventoryBalanceDetails> response = new([details], TotalCount: 1, Skip: 0, Take: 20);
        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson(response),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsInventoryApiClient apiClient = new(httpClient);

        ListResult<InventoryBalanceDetails> result = await apiClient.ListInventoryBalancesAsync(
            new ListInventoryBalancesRequest(),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Get, handler.RequestMethod);
        Assert.Equal("/api/wms/inventory/balances", handler.RequestPath);
        Assert.Equal(string.Empty, handler.RequestQuery);

        InventoryBalanceDetails item = Assert.Single(result.Items);
        Assert.Equal(details.Id, item.Id);
        Assert.Equal(details.Sku.Id, item.Sku.Id);
        Assert.Equal(details.Sku.Code, item.Sku.Code);
        Assert.Equal(details.Sku.BaseUom.Id, item.Sku.BaseUom.Id);
        Assert.Equal(details.Sku.BaseUom.Symbol, item.Sku.BaseUom.Symbol);
        Assert.Equal(details.StorageLocation.Id, item.StorageLocation.Id);
        Assert.Equal(details.StorageLocation.Warehouse.Id, item.StorageLocation.Warehouse.Id);
        Assert.Equal(details.StorageLocation.Warehouse.Code, item.StorageLocation.Warehouse.Code);
    }

    [Fact]
    public async Task ListInventoryBalancesAsync_WhenRequestHasExplicitValues_IncludesQueryParametersAndPropagatesCancellation()
    {
        Guid stockKeepingUnitId = Guid.Parse("018f0000-0000-7000-8000-000000000101");
        Guid storageLocationId = Guid.Parse("018f0000-0000-7000-8000-000000000201");
        Guid warehouseId = Guid.Parse("018f0000-0000-7000-8000-000000000301");
        ListResult<InventoryBalanceDetails> response = new([], TotalCount: 0, Skip: 0, Take: 50);
        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson(response),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsInventoryApiClient apiClient = new(httpClient);
        using CancellationTokenSource cancellationTokenSource = new();

        await apiClient.ListInventoryBalancesAsync(
            new ListInventoryBalancesRequest
            {
                Skip = 0,
                Take = 50,
                SortBy = InventoryBalanceSortBy.SkuCode,
                SortDescending = false,
                StockKeepingUnitId = stockKeepingUnitId,
                StorageLocationId = storageLocationId,
                WarehouseId = warehouseId
            },
            cancellationTokenSource.Token);

        Dictionary<string, string> query = ParseQuery(handler.RequestQuery);

        Assert.Equal("0", query["skip"]);
        Assert.Equal("50", query["take"]);
        Assert.Equal(InventoryBalanceSortBy.SkuCode, query["sortBy"]);
        Assert.Equal("false", query["sortDescending"]);
        Assert.Equal(stockKeepingUnitId.ToString(), query["stockKeepingUnitId"]);
        Assert.Equal(storageLocationId.ToString(), query["storageLocationId"]);
        Assert.Equal(warehouseId.ToString(), query["warehouseId"]);
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
    public async Task TryAdjustInventoryBalanceAsync_WhenSuccessful_PostsRequestBodyAndMapsVersion()
    {
        Guid stockKeepingUnitId = Guid.Parse("018f0000-0000-7000-8000-000000000101");
        Guid storageLocationId = Guid.Parse("018f0000-0000-7000-8000-000000000201");
        InventoryBalanceDetails details = CreateInventoryBalanceDetails(
            stockKeepingUnitId: stockKeepingUnitId,
            storageLocationId: storageLocationId,
            quantity: 14,
            balanceVersion: "AAAAAAAAB9I=");
        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson(details),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsInventoryApiClient apiClient = new(httpClient);

        ApiResult<InventoryBalanceDetails> result = await apiClient.TryAdjustInventoryBalanceAsync(
            new AdjustInventoryBalanceRequest(
                stockKeepingUnitId,
                storageLocationId,
                CountedQuantity: 14,
                Reason: "Cycle count correction",
                ExpectedBalanceVersion: "AAAAAAAAB9E="),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(14, result.Value.Quantity);
        Assert.Equal("AAAAAAAAB9I=", result.Value.BalanceVersion);
        Assert.Equal(HttpMethod.Post, handler.RequestMethod);
        Assert.Equal("/api/wms/inventory/adjustments", handler.RequestPath);

        using JsonDocument requestBody = JsonDocument.Parse(handler.RequestBody);
        JsonElement root = requestBody.RootElement;
        Assert.Equal(stockKeepingUnitId, root.GetProperty("stockKeepingUnitId").GetGuid());
        Assert.Equal(storageLocationId, root.GetProperty("storageLocationId").GetGuid());
        Assert.Equal(14, root.GetProperty("countedQuantity").GetDecimal());
        Assert.Equal("Cycle count correction", root.GetProperty("reason").GetString());
        Assert.Equal("AAAAAAAAB9E=", root.GetProperty("expectedBalanceVersion").GetString());
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

    private static string SerializeJson<T>(T value)
    {
        return JsonSerializer.Serialize(value, JsonOptions);
    }

    private static InventoryBalanceDetails CreateInventoryBalanceDetails(
        Guid? inventoryBalanceId = null,
        Guid? stockKeepingUnitId = null,
        Guid? storageLocationId = null,
        decimal quantity = 10,
        string balanceVersion = "AAAAAAAAB9E=")
    {
        return new InventoryBalanceDetails(
            inventoryBalanceId ?? Guid.Parse("018f0000-0000-7000-8000-000000000001"),
            quantity,
            DateTimeOffset.Parse("2026-06-17T09:00:00Z"),
            DateTimeOffset.Parse("2026-06-17T10:00:00Z"),
            balanceVersion,
            new InventoryBalanceDetails.StockKeepingUnitInfo(
                stockKeepingUnitId ?? Guid.Parse("018f0000-0000-7000-8000-000000000101"),
                "SKU-001",
                "Widget",
                new InventoryBalanceDetails.UnitOfMeasureInfo(
                    Guid.Parse("018f0000-0000-7000-8000-000000000111"),
                    "EA",
                    "ea")),
            new InventoryBalanceDetails.StorageLocationInfo(
                storageLocationId ?? Guid.Parse("018f0000-0000-7000-8000-000000000201"),
                "A-01-01",
                "A-01-01",
                new InventoryBalanceDetails.WarehouseInfo(
                    Guid.Parse("018f0000-0000-7000-8000-000000000301"),
                    "MAIN",
                    "Main Warehouse")));
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
