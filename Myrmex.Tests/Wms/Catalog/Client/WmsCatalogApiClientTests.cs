using Myrmex.WebApp.Wms.Catalog;
using Myrmex.WebApp.Wms.Api;
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
    public async Task TryCreateUnitOfMeasureAsync_WhenSuccessful_PostsToUomRouteAndReturnsDetails()
    {
        // Arrange
        const string responseJson = """
            {
              "id": "018f0000-0000-7000-8000-000000000002",
              "code": "EA",
              "name": "Each",
              "symbol": "ea",
              "isActive": true,
              "createdAtUtc": "2026-06-09T00:00:00+00:00",
              "updatedAtUtc": null
            }
            """;

        CapturingHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            responseJson,
            "application/json");

        using HttpClient httpClient = new(handler)
        {
            BaseAddress = new Uri("https://myrmex.test")
        };

        WmsCatalogApiClient apiClient = new(httpClient);

        CreateUnitOfMeasureRequest request = new(
            Code: "EA",
            Name: "Each",
            Symbol: "ea");

        // Act
        ApiResult<UnitOfMeasureDetails> result = await apiClient.TryCreateUnitOfMeasureAsync(
            request,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        Assert.Equal(Guid.Parse("018f0000-0000-7000-8000-000000000002"), result.Value.Id);
        Assert.Equal("EA", result.Value.Code);
        Assert.Equal("Each", result.Value.Name);
        Assert.Equal("ea", result.Value.Symbol);
        Assert.True(result.Value.IsActive);
        Assert.Null(result.Value.UpdatedAtUtc);

        Assert.Equal(HttpMethod.Post, handler.RequestMethod);
        Assert.Equal("/api/wms/catalog/uoms", handler.RequestPathAndQuery);
        Assert.NotNull(handler.RequestContent);
        Assert.Contains("\"code\":\"EA\"", handler.RequestContent);
        Assert.Contains("\"name\":\"Each\"", handler.RequestContent);
        Assert.Contains("\"symbol\":\"ea\"", handler.RequestContent);
    }

    [Fact]
    public async Task TryCreateUnitOfMeasureAsync_WhenProblemDetailsReturned_ReturnsFailureResult()
    {
        // Arrange
        const string problemJson = """
            {
              "type": "https://httpstatuses.com/409",
              "title": "Conflict",
              "status": 409,
              "detail": "Unit of measure with the same code already exists.",
              "code": "UnitOfMeasure.CodeAlreadyExists",
              "field": "code"
            }
            """;

        using HttpClient httpClient = CreateHttpClient(
            HttpStatusCode.Conflict,
            problemJson,
            "application/problem+json");

        WmsCatalogApiClient apiClient = new(httpClient);

        CreateUnitOfMeasureRequest request = new(
            Code: "EA",
            Name: "Each",
            Symbol: "ea");

        // Act
        ApiResult<UnitOfMeasureDetails> result = await apiClient.TryCreateUnitOfMeasureAsync(
            request,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.NotNull(result.Error);

        Assert.Equal(409, result.Error.Status);
        Assert.Equal("Unit of measure with the same code already exists.", result.Error.Message);
        Assert.Equal("UnitOfMeasure.CodeAlreadyExists", result.Error.Extensions["code"]);
        Assert.Equal("code", result.Error.Extensions["field"]);
    }

    [Fact]
    public async Task ListUnitsOfMeasureAsync_WhenSuccessful_GetsFromUomRouteAndReturnsDetails()
    {
        // Arrange
        const string responseJson = """
            {
              "items": [
                {
                  "id": "018f0000-0000-7000-8000-000000000002",
                  "code": "EA",
                  "name": "Each",
                  "symbol": "ea",
                  "isActive": true,
                  "createdAtUtc": "2026-06-09T00:00:00+00:00",
                  "updatedAtUtc": null
                }
              ],
              "totalCount": 1,
              "skip": 0,
              "take": 20
            }
            """;

        CapturingHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            responseJson,
            "application/json");

        using HttpClient httpClient = new(handler)
        {
            BaseAddress = new Uri("https://myrmex.test")
        };

        WmsCatalogApiClient apiClient = new(httpClient);

        ListRequest request = new(
            SearchText: "EA",
            SortBy: "code",
            IncludeInactive: true);

        // Act
        ListResult<UnitOfMeasureDetails> result = await apiClient.ListUnitsOfMeasureAsync(
            request,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result.TotalCount);
        UnitOfMeasureDetails details = Assert.Single(result.Items);
        Assert.Equal("EA", details.Code);
        Assert.Equal("Each", details.Name);
        Assert.Equal("ea", details.Symbol);

        Assert.Equal(HttpMethod.Get, handler.RequestMethod);
        Assert.Equal(
            "/api/wms/catalog/uoms?skip=0&take=20&searchText=EA&sortBy=code&sortDescending=false&includeInactive=true",
            handler.RequestPathAndQuery);
    }

    [Fact]
    public async Task ListUnitsOfMeasureAsync_WhenProblemDetailsReturned_ThrowsApiException()
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
            apiClient.ListUnitsOfMeasureAsync(
                request,
                TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal(500, exception.Status);
        Assert.Equal("Unexpected catalog API failure.", exception.Message);
        Assert.Equal("Error.Unknown", exception.Extensions["code"]);
    }

    [Fact]
    public async Task GetUnitOfMeasureByIdAsync_WhenSuccessful_GetsFromUomRouteAndReturnsDetails()
    {
        // Arrange
        const string responseJson = """
            {
              "id": "018f0000-0000-7000-8000-000000000002",
              "code": "EA",
              "name": "Each",
              "symbol": "ea",
              "isActive": true,
              "createdAtUtc": "2026-06-09T00:00:00+00:00",
              "updatedAtUtc": null
            }
            """;

        Guid unitOfMeasureId = Guid.Parse("018f0000-0000-7000-8000-000000000002");

        CapturingHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            responseJson,
            "application/json");

        using HttpClient httpClient = new(handler)
        {
            BaseAddress = new Uri("https://myrmex.test")
        };

        WmsCatalogApiClient apiClient = new(httpClient);

        // Act
        UnitOfMeasureDetails result = await apiClient.GetUnitOfMeasureByIdAsync(
            unitOfMeasureId,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(unitOfMeasureId, result.Id);
        Assert.Equal("EA", result.Code);
        Assert.Equal("Each", result.Name);

        Assert.Equal(HttpMethod.Get, handler.RequestMethod);
        Assert.Equal($"/api/wms/catalog/uoms/{unitOfMeasureId}", handler.RequestPathAndQuery);
    }

    [Fact]
    public async Task GetUnitOfMeasureByIdAsync_WhenProblemDetailsReturned_ThrowsApiException()
    {
        // Arrange
        const string problemJson = """
            {
              "type": "https://httpstatuses.com/404",
              "title": "Not Found",
              "status": 404,
              "detail": "Unit of measure was not found.",
              "code": "UnitOfMeasure.NotFound"
            }
            """;

        using HttpClient httpClient = CreateHttpClient(
            HttpStatusCode.NotFound,
            problemJson,
            "application/problem+json");

        WmsCatalogApiClient apiClient = new(httpClient);

        // Act
        ApiException exception = await Assert.ThrowsAsync<ApiException>(() =>
            apiClient.GetUnitOfMeasureByIdAsync(
                Guid.NewGuid(),
                TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal(404, exception.Status);
        Assert.Equal("Unit of measure was not found.", exception.Message);
        Assert.Equal("UnitOfMeasure.NotFound", exception.Extensions["code"]);
    }

    [Fact]
    public async Task TryUpdateUnitOfMeasureDetailsAsync_WhenSuccessful_PutsToUomRouteAndReturnsDetails()
    {
        // Arrange
        const string responseJson = """
            {
              "id": "018f0000-0000-7000-8000-000000000002",
              "code": "EA",
              "name": "Each Updated",
              "symbol": "each",
              "isActive": true,
              "createdAtUtc": "2026-06-09T00:00:00+00:00",
              "updatedAtUtc": "2026-06-09T01:00:00+00:00"
            }
            """;

        Guid unitOfMeasureId = Guid.Parse("018f0000-0000-7000-8000-000000000002");

        CapturingHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            responseJson,
            "application/json");

        using HttpClient httpClient = new(handler)
        {
            BaseAddress = new Uri("https://myrmex.test")
        };

        WmsCatalogApiClient apiClient = new(httpClient);

        UpdateUnitOfMeasureDetailsRequest request = new(
            Name: "Each Updated",
            Symbol: "each");

        // Act
        ApiResult<UnitOfMeasureDetails> result = await apiClient.TryUpdateUnitOfMeasureDetailsAsync(
            unitOfMeasureId,
            request,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        Assert.Equal(unitOfMeasureId, result.Value.Id);
        Assert.Equal("EA", result.Value.Code);
        Assert.Equal("Each Updated", result.Value.Name);
        Assert.Equal("each", result.Value.Symbol);
        Assert.NotNull(result.Value.UpdatedAtUtc);

        Assert.Equal(HttpMethod.Put, handler.RequestMethod);
        Assert.Equal($"/api/wms/catalog/uoms/{unitOfMeasureId}", handler.RequestPathAndQuery);
        Assert.NotNull(handler.RequestContent);
        Assert.Contains("\"name\":\"Each Updated\"", handler.RequestContent);
        Assert.Contains("\"symbol\":\"each\"", handler.RequestContent);
        Assert.DoesNotContain("\"code\"", handler.RequestContent);
    }

    [Fact]
    public async Task TryUpdateUnitOfMeasureDetailsAsync_WhenProblemDetailsReturned_ReturnsFailureResult()
    {
        // Arrange
        const string problemJson = """
            {
              "type": "https://httpstatuses.com/404",
              "title": "Not Found",
              "status": 404,
              "detail": "Unit of measure was not found.",
              "code": "UnitOfMeasure.NotFound"
            }
            """;

        using HttpClient httpClient = CreateHttpClient(
            HttpStatusCode.NotFound,
            problemJson,
            "application/problem+json");

        WmsCatalogApiClient apiClient = new(httpClient);

        UpdateUnitOfMeasureDetailsRequest request = new(
            Name: "Updated Each",
            Symbol: null);

        // Act
        ApiResult<UnitOfMeasureDetails> result = await apiClient.TryUpdateUnitOfMeasureDetailsAsync(
            Guid.NewGuid(),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);

        Assert.Equal(404, result.Error.Status);
        Assert.Equal("Unit of measure was not found.", result.Error.Message);
        Assert.Equal("UnitOfMeasure.NotFound", result.Error.Extensions["code"]);
    }

    [Fact]
    public async Task TryDeactivateUnitOfMeasureAsync_WhenSuccessful_PostsToUomDeactivateRouteAndReturnsDetails()
    {
        // Arrange
        const string responseJson = """
            {
              "id": "018f0000-0000-7000-8000-000000000002",
              "code": "EA",
              "name": "Each",
              "symbol": "ea",
              "isActive": false,
              "createdAtUtc": "2026-06-09T00:00:00+00:00",
              "updatedAtUtc": "2026-06-09T01:00:00+00:00"
            }
            """;

        Guid unitOfMeasureId = Guid.Parse("018f0000-0000-7000-8000-000000000002");

        CapturingHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            responseJson,
            "application/json");

        using HttpClient httpClient = new(handler)
        {
            BaseAddress = new Uri("https://myrmex.test")
        };

        WmsCatalogApiClient apiClient = new(httpClient);

        // Act
        ApiResult<UnitOfMeasureDetails> result = await apiClient.TryDeactivateUnitOfMeasureAsync(
            unitOfMeasureId,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.False(result.Value.IsActive);

        Assert.Equal(HttpMethod.Post, handler.RequestMethod);
        Assert.Equal($"/api/wms/catalog/uoms/{unitOfMeasureId}/deactivate", handler.RequestPathAndQuery);
    }

    [Fact]
    public async Task TryDeactivateUnitOfMeasureAsync_WhenProblemDetailsReturned_ReturnsFailureResult()
    {
        // Arrange
        const string problemJson = """
            {
              "type": "https://httpstatuses.com/404",
              "title": "Not Found",
              "status": 404,
              "detail": "Unit of measure was not found.",
              "code": "UnitOfMeasure.NotFound"
            }
            """;

        using HttpClient httpClient = CreateHttpClient(
            HttpStatusCode.NotFound,
            problemJson,
            "application/problem+json");

        WmsCatalogApiClient apiClient = new(httpClient);

        // Act
        ApiResult<UnitOfMeasureDetails> result = await apiClient.TryDeactivateUnitOfMeasureAsync(
            Guid.NewGuid(),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);

        Assert.Equal(404, result.Error.Status);
        Assert.Equal("Unit of measure was not found.", result.Error.Message);
        Assert.Equal("UnitOfMeasure.NotFound", result.Error.Extensions["code"]);
    }

    [Fact]
    public async Task TryReactivateUnitOfMeasureAsync_WhenSuccessful_PostsToUomReactivateRouteAndReturnsDetails()
    {
        // Arrange
        const string responseJson = """
            {
              "id": "018f0000-0000-7000-8000-000000000002",
              "code": "EA",
              "name": "Each",
              "symbol": "ea",
              "isActive": true,
              "createdAtUtc": "2026-06-09T00:00:00+00:00",
              "updatedAtUtc": "2026-06-09T01:00:00+00:00"
            }
            """;

        Guid unitOfMeasureId = Guid.Parse("018f0000-0000-7000-8000-000000000002");

        CapturingHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            responseJson,
            "application/json");

        using HttpClient httpClient = new(handler)
        {
            BaseAddress = new Uri("https://myrmex.test")
        };

        WmsCatalogApiClient apiClient = new(httpClient);

        // Act
        ApiResult<UnitOfMeasureDetails> result = await apiClient.TryReactivateUnitOfMeasureAsync(
            unitOfMeasureId,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.IsActive);

        Assert.Equal(HttpMethod.Post, handler.RequestMethod);
        Assert.Equal($"/api/wms/catalog/uoms/{unitOfMeasureId}/reactivate", handler.RequestPathAndQuery);
    }

    [Fact]
    public async Task TryReactivateUnitOfMeasureAsync_WhenProblemDetailsReturned_ReturnsFailureResult()
    {
        // Arrange
        const string problemJson = """
            {
              "type": "https://httpstatuses.com/404",
              "title": "Not Found",
              "status": 404,
              "detail": "Unit of measure was not found.",
              "code": "UnitOfMeasure.NotFound"
            }
            """;

        using HttpClient httpClient = CreateHttpClient(
            HttpStatusCode.NotFound,
            problemJson,
            "application/problem+json");

        WmsCatalogApiClient apiClient = new(httpClient);

        // Act
        ApiResult<UnitOfMeasureDetails> result = await apiClient.TryReactivateUnitOfMeasureAsync(
            Guid.NewGuid(),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);

        Assert.Equal(404, result.Error.Status);
        Assert.Equal("Unit of measure was not found.", result.Error.Message);
        Assert.Equal("UnitOfMeasure.NotFound", result.Error.Extensions["code"]);
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

    private sealed class CapturingHttpMessageHandler(
        HttpStatusCode statusCode,
        string content,
        string mediaType) : HttpMessageHandler
    {
        public HttpMethod? RequestMethod { get; private set; }

        public string? RequestPathAndQuery { get; private set; }

        public string? RequestContent { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestMethod = request.Method;
            RequestPathAndQuery = request.RequestUri?.PathAndQuery;

            if (request.Content is not null)
            {
                RequestContent = await request.Content.ReadAsStringAsync(cancellationToken);
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
