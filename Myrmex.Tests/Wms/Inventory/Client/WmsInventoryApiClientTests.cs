using Myrmex.Shared.Wms.Inventory;
using Myrmex.WebApp.Wms.Api;
using Myrmex.WebApp.Wms.Inventory;
using System.Text;
using System.Web;

namespace Myrmex.Tests.Wms.Inventory.Client;

public sealed class WmsInventoryApiClientTests
{
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
