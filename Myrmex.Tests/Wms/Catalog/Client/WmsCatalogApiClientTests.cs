using Myrmex.WebApp.Wms.Catalog;
using System.Text;

namespace Myrmex.Tests.Wms.Catalog.Client;

public sealed class WmsCatalogApiClientTests
{
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
