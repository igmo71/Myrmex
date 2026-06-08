using Myrmex.WebApp.Wms.Catalog;
using System.Text;

namespace Myrmex.Tests.Wms.Catalog.Client;

public sealed class WmsCatalogApiClientTests
{
    [Fact]
    public async Task ListStockKeepingUnitsAsync_WhenProblemDetailsReturned_ThrowsApiException()
    {
        // Arrange
        const string problemJson = """
            {
              "type": "https://httpstatuses.com/500",
              "title": "Internal Server Error",
              "status": 500,
              "detail": "Unexpected catalog API failure.",
              "code": "Error.Unknown"
            }
            """;

        using HttpClient httpClient = CreateHttpClient(
            HttpStatusCode.InternalServerError,
            problemJson,
            "application/problem+json");

        WmsCatalogApiClient apiClient = new(httpClient);

        ListRequest request = new();

        // Act
        ApiException exception = await Assert.ThrowsAsync<ApiException>(() =>
            apiClient.ListStockKeepingUnitsAsync(
                request,
                TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal(500, exception.Status);
        Assert.Equal("Unexpected catalog API failure.", exception.Message);
        Assert.Equal("Error.Unknown", exception.Extensions["code"]);
    }

    [Fact]
    public async Task GetStockKeepingUnitByIdAsync_WhenProblemDetailsReturned_ThrowsApiException()
    {
        // Arrange
        const string problemJson = """
            {
              "type": "https://httpstatuses.com/404",
              "title": "Not Found",
              "status": 404,
              "detail": "Stock keeping unit was not found.",
              "code": "StockKeepingUnit.NotFound"
            }
            """;

        using HttpClient httpClient = CreateHttpClient(
            HttpStatusCode.NotFound,
            problemJson,
            "application/problem+json");

        WmsCatalogApiClient apiClient = new(httpClient);

        // Act
        ApiException exception = await Assert.ThrowsAsync<ApiException>(() =>
            apiClient.GetStockKeepingUnitByIdAsync(
                Guid.NewGuid(),
                TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal(404, exception.Status);
        Assert.Equal("Stock keeping unit was not found.", exception.Message);
        Assert.Equal("StockKeepingUnit.NotFound", exception.Extensions["code"]);
    }

    [Fact]
    public async Task TryCreateStockKeepingUnitAsync_WhenProblemDetailsReturned_ReturnsFailureResult()
    {
        // Arrange
        const string problemJson = """
            {
              "type": "https://httpstatuses.com/409",
              "title": "Conflict",
              "status": 409,
              "detail": "SKU with the same code already exists.",
              "code": "StockKeepingUnit.CodeAlreadyExists",
              "field": "code"
            }
            """;

        using HttpClient httpClient = CreateHttpClient(
            HttpStatusCode.Conflict,
            problemJson,
            "application/problem+json");

        WmsCatalogApiClient apiClient = new(httpClient);

        CreateStockKeepingUnitRequest request = new(
            Code: "ITEM-001",
            Name: "Widget",
            Description: null);

        // Act
        ApiResult<StockKeepingUnitDetails> result = await apiClient.TryCreateStockKeepingUnitAsync(
            request,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.NotNull(result.Error);

        Assert.Equal(409, result.Error.Status);
        Assert.Equal("SKU with the same code already exists.", result.Error.Message);
        Assert.Equal("StockKeepingUnit.CodeAlreadyExists", result.Error.Extensions["code"]);
        Assert.Equal("code", result.Error.Extensions["field"]);
    }

    [Fact]
    public async Task TryUpdateStockKeepingUnitDetailsAsync_WhenProblemDetailsReturned_ReturnsFailureResult()
    {
        // Arrange
        const string problemJson = """
            {
              "type": "https://httpstatuses.com/404",
              "title": "Not Found",
              "status": 404,
              "detail": "Stock keeping unit was not found.",
              "code": "StockKeepingUnit.NotFound"
            }
            """;

        using HttpClient httpClient = CreateHttpClient(
            HttpStatusCode.NotFound,
            problemJson,
            "application/problem+json");

        WmsCatalogApiClient apiClient = new(httpClient);

        UpdateStockKeepingUnitDetailsRequest request = new(
            Name: "Updated Widget",
            Description: null);

        // Act
        ApiResult<StockKeepingUnitDetails> result = await apiClient.TryUpdateStockKeepingUnitDetailsAsync(
            Guid.NewGuid(),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);

        Assert.Equal(404, result.Error.Status);
        Assert.Equal("Stock keeping unit was not found.", result.Error.Message);
        Assert.Equal("StockKeepingUnit.NotFound", result.Error.Extensions["code"]);
    }

    [Fact]
    public async Task TryDeactivateStockKeepingUnitAsync_WhenProblemDetailsReturned_ReturnsFailureResult()
    {
        // Arrange
        const string problemJson = """
            {
              "type": "https://httpstatuses.com/404",
              "title": "Not Found",
              "status": 404,
              "detail": "Stock keeping unit was not found.",
              "code": "StockKeepingUnit.NotFound"
            }
            """;

        using HttpClient httpClient = CreateHttpClient(
            HttpStatusCode.NotFound,
            problemJson,
            "application/problem+json");

        WmsCatalogApiClient apiClient = new(httpClient);

        // Act
        ApiResult<StockKeepingUnitDetails> result = await apiClient.TryDeactivateStockKeepingUnitAsync(
            Guid.NewGuid(),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);

        Assert.Equal(404, result.Error.Status);
        Assert.Equal("Stock keeping unit was not found.", result.Error.Message);
        Assert.Equal("StockKeepingUnit.NotFound", result.Error.Extensions["code"]);
    }

    [Fact]
    public async Task ListStockKeepingUnitsAsync_WhenMalformedErrorReturned_ThrowsApiExceptionWithFallbackMessage()
    {
        // Arrange
        using HttpClient httpClient = CreateHttpClient(
            HttpStatusCode.BadRequest,
            "not a valid problem details json",
            "application/problem+json");

        WmsCatalogApiClient apiClient = new(httpClient);

        ListRequest request = new();

        // Act
        ApiException exception = await Assert.ThrowsAsync<ApiException>(() =>
            apiClient.ListStockKeepingUnitsAsync(
                request,
                TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal(400, exception.Status);
        Assert.Equal(
            "API request failed for GET '/api/wms/catalog/skus?skip=0&take=20&sortDescending=false&includeInactive=false'. Status code: 400 BadRequest.",
            exception.Message);
        Assert.Empty(exception.Extensions);
    }

    [Fact]
    public async Task GetStockKeepingUnitByIdAsync_WhenMalformedErrorReturned_ThrowsApiExceptionWithFallbackMessage()
    {
        // Arrange
        Guid stockKeepingUnitId = Guid.Parse("018f0000-0000-7000-8000-000000000001");

        using HttpClient httpClient = CreateHttpClient(
            HttpStatusCode.BadRequest,
            "not a valid problem details json",
            "application/problem+json");

        WmsCatalogApiClient apiClient = new(httpClient);

        // Act
        ApiException exception = await Assert.ThrowsAsync<ApiException>(() =>
            apiClient.GetStockKeepingUnitByIdAsync(
                stockKeepingUnitId,
                TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal(400, exception.Status);
        Assert.Equal(
            $"API request failed for GET '/api/wms/catalog/skus/{stockKeepingUnitId}'. Status code: 400 BadRequest.",
            exception.Message);
        Assert.Empty(exception.Extensions);
    }

    [Fact]
    public async Task TryCreateStockKeepingUnitAsync_WhenMalformedErrorReturned_ReturnsFallbackFailureResult()
    {
        // Arrange
        using HttpClient httpClient = CreateHttpClient(
            HttpStatusCode.BadRequest,
            "not a valid problem details json",
            "application/problem+json");

        WmsCatalogApiClient apiClient = new(httpClient);

        CreateStockKeepingUnitRequest request = new(
            Code: "ITEM-001",
            Name: "Widget",
            Description: null);

        // Act
        ApiResult<StockKeepingUnitDetails> result = await apiClient.TryCreateStockKeepingUnitAsync(
            request,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);

        Assert.Equal(400, result.Error.Status);
        Assert.Equal(
            "API request failed for POST '/api/wms/catalog/skus'. Status code: 400 BadRequest.",
            result.Error.Message);
        Assert.Empty(result.Error.Extensions);
    }

    [Fact]
    public async Task TryReactivateStockKeepingUnitAsync_WhenMalformedErrorReturned_ReturnsFallbackFailureResult()
    {
        // Arrange
        Guid stockKeepingUnitId = Guid.Parse("018f0000-0000-7000-8000-000000000001");

        using HttpClient httpClient = CreateHttpClient(
            HttpStatusCode.BadRequest,
            "not a valid problem details json",
            "application/problem+json");

        WmsCatalogApiClient apiClient = new(httpClient);

        // Act
        ApiResult<StockKeepingUnitDetails> result = await apiClient.TryReactivateStockKeepingUnitAsync(
            stockKeepingUnitId,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);

        Assert.Equal(400, result.Error.Status);
        Assert.Equal(
            $"API request failed for POST '/api/wms/catalog/skus/{stockKeepingUnitId}/reactivate'. Status code: 400 BadRequest.",
            result.Error.Message);
        Assert.Empty(result.Error.Extensions);
    }

    private static HttpClient CreateHttpClient(
        HttpStatusCode statusCode,
        string content,
        string mediaType)
    {
        StubHttpMessageHandler handler = new(statusCode, content, mediaType);

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
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage response = new(statusCode)
            {
                Content = new StringContent(
                    content,
                    Encoding.UTF8,
                    mediaType)
            };

            return Task.FromResult(response);
        }
    }
}
