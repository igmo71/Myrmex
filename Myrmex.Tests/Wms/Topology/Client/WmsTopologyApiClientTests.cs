using Myrmex.WebApp.Wms.Topology;
using System.Text;

namespace Myrmex.Tests.Wms.Topology.Client;

public sealed class WmsTopologyApiClientTests
{
    [Fact]
    public async Task TryCreateWarehouseAsync_WhenProblemDetailsReturned_ReturnsFailureResult()
    {
        // Arrange
        const string problemJson = """
            {
              "type": "https://httpstatuses.com/409",
              "title": "Conflict",
              "status": 409,
              "detail": "Warehouse with the same code already exists.",
              "code": "Warehouse.CodeAlreadyExists",
              "field": "code"
            }
            """;

        using HttpClient httpClient = CreateHttpClient(
            HttpStatusCode.Conflict,
            problemJson,
            "application/problem+json");

        WmsTopologyApiClient apiClient = new(httpClient);

        CreateWarehouseRequest request = new(
            Code: "MAIN",
            Name: "Main Warehouse",
            Description: null);

        // Act
        ApiResult<WarehouseDetails> result = await apiClient.TryCreateWarehouseAsync(
            request,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.NotNull(result.Error);

        Assert.Equal(409, result.Error.Status);
        Assert.Equal("Warehouse with the same code already exists.", result.Error.Message);
        Assert.Equal("Warehouse.CodeAlreadyExists", result.Error.Extensions["code"]);
        Assert.Equal("code", result.Error.Extensions["field"]);
    }

    [Fact]
    public async Task ListWarehousesAsync_WhenProblemDetailsReturned_ThrowsApiException()
    {
        // Arrange
        const string problemJson = """
            {
              "type": "https://httpstatuses.com/500",
              "title": "Internal Server Error",
              "status": 500,
              "detail": "Unexpected API failure.",
              "code": "Error.Unknown"
            }
            """;

        using HttpClient httpClient = CreateHttpClient(
            HttpStatusCode.InternalServerError,
            problemJson,
            "application/problem+json");

        WmsTopologyApiClient apiClient = new(httpClient);

        ListRequest request = new();

        // Act
        ApiException exception = await Assert.ThrowsAsync<ApiException>(() =>
            apiClient.ListWarehousesAsync(
                request,
                TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal(500, exception.Status);
        Assert.Equal("Unexpected API failure.", exception.Message);
        Assert.Equal("Error.Unknown", exception.Extensions["code"]);
    }

    [Fact]
    public async Task TryCreateWarehouseAsync_WhenMalformedErrorReturned_ReturnsFallbackFailureResult()
    {
        // Arrange
        using HttpClient httpClient = CreateHttpClient(
            HttpStatusCode.BadRequest,
            "not a valid problem details json",
            "application/problem+json");

        WmsTopologyApiClient apiClient = new(httpClient);

        CreateWarehouseRequest request = new(
            Code: "MAIN",
            Name: "Main Warehouse",
            Description: null);

        // Act
        ApiResult<WarehouseDetails> result = await apiClient.TryCreateWarehouseAsync(
            request,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);

        Assert.Equal(400, result.Error.Status);
        Assert.Equal(
            "API request failed for POST '/api/wms/topology/warehouses'. Status code: 400 BadRequest.",
            result.Error.Message);
        Assert.Empty(result.Error.Extensions);
    }

    [Fact]
    public async Task ListWarehousesAsync_WhenMalformedErrorReturned_ThrowsApiExceptionWithFallbackMessage()
    {
        // Arrange
        using HttpClient httpClient = CreateHttpClient(
            HttpStatusCode.BadRequest,
            "not a valid problem details json",
            "application/problem+json");

        WmsTopologyApiClient apiClient = new(httpClient);

        ListRequest request = new();

        // Act
        ApiException exception = await Assert.ThrowsAsync<ApiException>(() =>
            apiClient.ListWarehousesAsync(
                request,
                TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal(400, exception.Status);
        Assert.Equal(
            "API request failed for GET '/api/wms/topology/warehouses?skip=0&take=20&sortDescending=false&includeInactive=false'. Status code: 400 BadRequest.",
            exception.Message);
        Assert.Empty(exception.Extensions);
    }

    [Fact]
    public async Task TryCreateZoneAsync_WhenProblemDetailsReturned_ReturnsFailureResult()
    {
        // Arrange
        const string problemJson = """
            {
              "type": "https://httpstatuses.com/409",
              "title": "Conflict",
              "status": 409,
              "detail": "Zone with the same code already exists in this warehouse.",
              "code": "Zone.CodeAlreadyExists",
              "field": "code"
            }
            """;

        using HttpClient httpClient = CreateHttpClient(
            HttpStatusCode.Conflict,
            problemJson,
            "application/problem+json");

        WmsTopologyApiClient apiClient = new(httpClient);

        CreateZoneRequest request = new(
            Code: "ZONE-A",
            Name: "Zone A",
            Description: null);

        // Act
        ApiResult<ZoneDetails> result = await apiClient.TryCreateZoneAsync(
            warehouseId: Guid.NewGuid(),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.NotNull(result.Error);

        Assert.Equal(409, result.Error.Status);
        Assert.Equal("Zone with the same code already exists in this warehouse.", result.Error.Message);
        Assert.Equal("Zone.CodeAlreadyExists", result.Error.Extensions["code"]);
        Assert.Equal("code", result.Error.Extensions["field"]);
    }

    [Fact]
    public async Task ListZonesAsync_WhenProblemDetailsReturned_ThrowsApiException()
    {
        // Arrange
        const string problemJson = """
            {
              "type": "https://httpstatuses.com/500",
              "title": "Internal Server Error",
              "status": 500,
              "detail": "Unexpected zone API failure.",
              "code": "Error.Unknown"
            }
            """;

        using HttpClient httpClient = CreateHttpClient(
            HttpStatusCode.InternalServerError,
            problemJson,
            "application/problem+json");

        WmsTopologyApiClient apiClient = new(httpClient);

        ListRequest request = new();

        // Act
        ApiException exception = await Assert.ThrowsAsync<ApiException>(() =>
            apiClient.ListZonesAsync(
                warehouseId: Guid.NewGuid(),
                request,
                TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal(500, exception.Status);
        Assert.Equal("Unexpected zone API failure.", exception.Message);
        Assert.Equal("Error.Unknown", exception.Extensions["code"]);
    }

    [Fact]
    public async Task TryCreateStorageLocationAsync_WhenProblemDetailsReturned_ReturnsFailureResult()
    {
        // Arrange
        const string problemJson = """
            {
              "type": "https://httpstatuses.com/409",
              "title": "Conflict",
              "status": 409,
              "detail": "Storage location with the same code already exists in this warehouse.",
              "code": "StorageLocation.CodeAlreadyExists",
              "field": "code"
            }
            """;

        using HttpClient httpClient = CreateHttpClient(
            HttpStatusCode.Conflict,
            problemJson,
            "application/problem+json");

        WmsTopologyApiClient apiClient = new(httpClient);

        CreateStorageLocationRequest request = new(
            StorageLocationTypeId: Guid.NewGuid(),
            StorageLocationStatusId: Guid.NewGuid(),
            Code: "A-01-01",
            Name: "A-01-01",
            Description: null,
            IsPickable: true);

        // Act
        ApiResult<StorageLocationDetails> result = await apiClient.TryCreateStorageLocationAsync(
            warehouseId: Guid.NewGuid(),
            zoneId: Guid.NewGuid(),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.NotNull(result.Error);

        Assert.Equal(409, result.Error.Status);
        Assert.Equal("Storage location with the same code already exists in this warehouse.", result.Error.Message);
        Assert.Equal("StorageLocation.CodeAlreadyExists", result.Error.Extensions["code"]);
        Assert.Equal("code", result.Error.Extensions["field"]);
    }

    [Fact]
    public async Task ListStorageLocationsByWarehouseAsync_WhenProblemDetailsReturned_ThrowsApiException()
    {
        // Arrange
        const string problemJson = """
            {
              "type": "https://httpstatuses.com/500",
              "title": "Internal Server Error",
              "status": 500,
              "detail": "Unexpected storage location API failure.",
              "code": "Error.Unknown"
            }
            """;

        using HttpClient httpClient = CreateHttpClient(
            HttpStatusCode.InternalServerError,
            problemJson,
            "application/problem+json");

        WmsTopologyApiClient apiClient = new(httpClient);

        ListRequest request = new();

        // Act
        ApiException exception = await Assert.ThrowsAsync<ApiException>(() =>
            apiClient.ListStorageLocationsByWarehouseAsync(
                warehouseId: Guid.NewGuid(),
                request,
                TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal(500, exception.Status);
        Assert.Equal("Unexpected storage location API failure.", exception.Message);
        Assert.Equal("Error.Unknown", exception.Extensions["code"]);
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