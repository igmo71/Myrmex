using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Topology;
using Myrmex.WebApp.Wms.Api;
using Myrmex.WebApp.Wms.Topology;
using System.Text;
using System.Text.Json;
using LookupStorageLocationsRequest = Myrmex.Shared.Wms.Topology.LookupStorageLocationsRequest;
using StorageLocationLookupItem = Myrmex.Shared.Wms.Topology.StorageLocationLookupItem;

namespace Myrmex.Tests.Wms.Topology.Client;

public sealed class WmsTopologyApiClientTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task LookupWarehousesAsync_BuildsEncodedUrlMapsSharedItemsAndPropagatesCancellation()
    {
        WarehouseLookupItem details = new(
            Guid.Parse("018f0000-0000-7000-8000-000000000101"),
            "WH-A",
            "Warehouse A",
            true);
        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson<IReadOnlyList<WarehouseLookupItem>>([details]),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsTopologyApiClient apiClient = new(httpClient);
        using CancellationTokenSource cancellationTokenSource = new();

        IReadOnlyList<WarehouseLookupItem> result = await apiClient.LookupWarehousesAsync(
            new LookupWarehousesRequest
            {
                SearchText = "North & East",
                Take = 20,
                SelectableOnly = false
            },
            cancellationTokenSource.Token);

        Assert.Equal(details, Assert.Single(result));
        Assert.Equal("/api/wms/topology/warehouses/lookup", handler.RequestPath);
        Assert.Equal("?searchText=North+%26+East&take=20&selectableOnly=false", handler.RequestQuery);
        Assert.True(handler.RequestCancellationToken.CanBeCanceled);
    }

    [Fact]
    public async Task ListWarehousesAsync_WhenSuccessful_BuildsFeatureListUrlAndOmitsNulls()
    {
        WarehouseDetails details = new(
            Guid.Parse("018f0000-0000-7000-8000-000000000101"),
            "WH-A",
            "Warehouse A",
            null,
            true,
            DateTimeOffset.Parse("2026-06-17T09:00:00Z"),
            null);
        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson(new ListResult<WarehouseDetails>([details], 1, 0, 25)),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsTopologyApiClient apiClient = new(httpClient);

        ListResult<WarehouseDetails> result = await apiClient.ListWarehousesAsync(
            new ListWarehousesRequest
            {
                Take = 25,
                SortBy = WarehouseSortBy.Name
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(details, Assert.Single(result.Items));
        Assert.Equal("/api/wms/topology/warehouses", handler.RequestPath);
        Assert.Equal("?take=25&sortBy=Name", handler.RequestQuery);
    }

    [Fact]
    public async Task ListZonesAsync_WhenSuccessful_BuildsNestedWarehouseRouteAndMapsDetails()
    {
        Guid warehouseId = Guid.Parse("018f0000-0000-7000-8000-000000000101");
        ZoneDetails details = new(
            Id: Guid.Parse("018f0000-0000-7000-8000-000000000201"),
            warehouseId,
            Code: "ZONE-A",
            Name: "Zone A",
            Description: "Picking zone",
            IsActive: true,
            CreatedAtUtc: DateTimeOffset.Parse("2026-06-17T09:00:00Z"),
            UpdatedAtUtc: null);

        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson(new ListResult<ZoneDetails>([details], TotalCount: 1, Skip: 5, Take: 10)),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsTopologyApiClient apiClient = new(httpClient);

        ListResult<ZoneDetails> result = await apiClient.ListZonesAsync(
            warehouseId,
            new ListZonesRequest
            {
                WarehouseId = warehouseId,
                Skip = 5,
                Take = 10,
                SearchText = "zone & pick",
                SortBy = ZoneSortBy.Code,
                SortDescending = false,
                IncludeInactive = true
            },
            TestContext.Current.CancellationToken);

        ZoneDetails item = Assert.Single(result.Items);
        Assert.Equal(details.Id, item.Id);
        Assert.Equal(warehouseId, item.WarehouseId);
        Assert.Equal("ZONE-A", item.Code);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(HttpMethod.Get, handler.RequestMethod);
        Assert.Equal($"/api/wms/topology/warehouses/{warehouseId}/zones", handler.RequestPath);
        Assert.Equal("?skip=5&take=10&searchText=zone+%26+pick&sortBy=Code&sortDescending=false&includeInactive=true", handler.RequestQuery);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ListStorageLocationsAsync_BuildsNestedRoutesAndAllFilters(bool byWarehouse)
    {
        Guid warehouseId = Guid.Parse("018f0000-0000-7000-8000-000000000101");
        Guid zoneId = Guid.Parse("018f0000-0000-7000-8000-000000000201");
        Guid typeId = Guid.Parse("018f0000-0000-7000-8000-000000000301");
        Guid statusId = Guid.Parse("018f0000-0000-7000-8000-000000000401");
        StorageLocationDetails details = new(
            Guid.Parse("018f0000-0000-7000-8000-000000000501"),
            warehouseId,
            zoneId,
            typeId,
            statusId,
            "A-01",
            "A 01",
            null,
            true,
            true,
            DateTimeOffset.Parse("2026-06-17T09:00:00Z"),
            null);
        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson(new ListResult<StorageLocationDetails>([details], 1, 10, 25)),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsTopologyApiClient apiClient = new(httpClient);
        using CancellationTokenSource cancellationTokenSource = new();
        ListStorageLocationsRequest request = new()
        {
            WarehouseId = warehouseId,
            ZoneId = zoneId,
            StorageLocationTypeId = typeId,
            StorageLocationStatusId = statusId,
            Skip = 10,
            Take = 25,
            SearchText = "A 01",
            SortBy = StorageLocationSortBy.UpdatedAtUtc,
            SortDescending = true,
            IncludeInactive = true
        };

        ListResult<StorageLocationDetails> result = byWarehouse
            ? await apiClient.ListStorageLocationsByWarehouseAsync(warehouseId, request, cancellationTokenSource.Token)
            : await apiClient.ListStorageLocationsByZoneAsync(zoneId, request, cancellationTokenSource.Token);

        Assert.Equal(details, Assert.Single(result.Items));
        Assert.True(handler.RequestCancellationToken.CanBeCanceled);
        Assert.Equal(
            byWarehouse
                ? $"/api/wms/topology/warehouses/{warehouseId}/locations"
                : $"/api/wms/topology/zones/{zoneId}/locations",
            handler.RequestPath);
        string routeFilter = byWarehouse ? $"&zoneId={zoneId}" : $"&warehouseId={warehouseId}";
        Assert.Equal(
            $"?skip=10&take=25&searchText=A+01&sortBy=UpdatedAtUtc&sortDescending=true&includeInactive=true" +
            routeFilter +
            $"&storageLocationTypeId={typeId}&storageLocationStatusId={statusId}",
            handler.RequestQuery);
    }

    [Fact]
    public async Task TryCreateStorageLocationAsync_WhenSuccessful_PostsNestedRouteRequestBodyAndMapsDetails()
    {
        Guid warehouseId = Guid.Parse("018f0000-0000-7000-8000-000000000101");
        Guid zoneId = Guid.Parse("018f0000-0000-7000-8000-000000000201");
        Guid typeId = Guid.Parse("018f0000-0000-7000-8000-000000000301");
        Guid statusId = Guid.Parse("018f0000-0000-7000-8000-000000000401");
        StorageLocationDetails details = new(
            Id: Guid.Parse("018f0000-0000-7000-8000-000000000501"),
            warehouseId,
            zoneId,
            typeId,
            statusId,
            Code: "A-01-01",
            Name: "A-01-01",
            Description: "Pick face",
            IsPickable: true,
            IsActive: true,
            CreatedAtUtc: DateTimeOffset.Parse("2026-06-17T09:00:00Z"),
            UpdatedAtUtc: null);

        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson(details),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsTopologyApiClient apiClient = new(httpClient);

        CreateStorageLocationRequest request = new(
            typeId,
            statusId,
            Code: " A-01-01 ",
            Name: "A-01-01",
            Description: "Pick face",
            IsPickable: true);

        ApiResult<StorageLocationDetails> result = await apiClient.TryCreateStorageLocationAsync(
            warehouseId,
            zoneId,
            request,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(details.Id, result.Value.Id);
        Assert.Equal(typeId, result.Value.StorageLocationTypeId);
        Assert.Equal(statusId, result.Value.StorageLocationStatusId);
        Assert.Equal(HttpMethod.Post, handler.RequestMethod);
        Assert.Equal(
            $"/api/wms/topology/warehouses/{warehouseId}/zones/{zoneId}/locations",
            handler.RequestPath);

        using JsonDocument requestBody = JsonDocument.Parse(handler.RequestBody);
        JsonElement root = requestBody.RootElement;
        Assert.Equal(typeId, root.GetProperty("storageLocationTypeId").GetGuid());
        Assert.Equal(statusId, root.GetProperty("storageLocationStatusId").GetGuid());
        Assert.Equal(" A-01-01 ", root.GetProperty("code").GetString());
        Assert.True(root.GetProperty("isPickable").GetBoolean());
    }

    [Fact]
    public async Task LookupStorageLocationsAsync_WhenSuccessful_BuildsWarehouseLookupRoute()
    {
        Guid warehouseId = Guid.Parse("018f0000-0000-7000-8000-000000000101");
        StorageLocationLookupItem details = new(
            Id: Guid.Parse("018f0000-0000-7000-8000-000000000501"),
            warehouseId,
            Code: "A-01-01",
            Name: "Pick Face",
            IsActive: false);

        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson<IReadOnlyList<StorageLocationLookupItem>>([details]),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsTopologyApiClient apiClient = new(httpClient);
        using CancellationTokenSource cancellationTokenSource = new();

        IReadOnlyList<StorageLocationLookupItem> result = await apiClient.LookupStorageLocationsAsync(
            warehouseId,
            new LookupStorageLocationsRequest
            {
                SearchText = "Pick",
                Take = 20,
                SelectableOnly = false,
                StorageLocationTypeCode = "INTERNAL_TRANSIT",
                ExcludeTransitTypes = true
            },
            cancellationTokenSource.Token);

        StorageLocationLookupItem item = Assert.Single(result);
        Assert.Equal(details.Id, item.Id);
        Assert.Equal(warehouseId, item.WarehouseId);
        Assert.False(item.IsActive);
        Assert.Equal(HttpMethod.Get, handler.RequestMethod);
        Assert.Equal($"/api/wms/topology/warehouses/{warehouseId}/locations/lookup", handler.RequestPath);
        Assert.Equal(
            "?searchText=Pick&take=20&selectableOnly=false&storageLocationTypeCode=INTERNAL_TRANSIT&excludeTransitTypes=true",
            handler.RequestQuery);
    }

    [Fact]
    public async Task ListStorageLocationTypesAsync_WhenIncludeInactiveTrue_GetsLookupRouteAndMapsDetails()
    {
        StorageLocationTypeDetails details = new(
            Id: Guid.Parse("018f0000-0000-7000-8000-000000000301"),
            Code: "PICK",
            Name: "Pick Face",
            Description: null,
            IsSystem: true,
            IsActive: false,
            SortOrder: 10);

        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson<IReadOnlyList<StorageLocationTypeDetails>>([details]),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsTopologyApiClient apiClient = new(httpClient);

        IReadOnlyList<StorageLocationTypeDetails> result =
            await apiClient.ListStorageLocationTypesAsync(
                includeInactive: true,
                TestContext.Current.CancellationToken);

        StorageLocationTypeDetails item = Assert.Single(result);
        Assert.Equal(details.Id, item.Id);
        Assert.Equal("PICK", item.Code);
        Assert.False(item.IsActive);
        Assert.Equal(HttpMethod.Get, handler.RequestMethod);
        Assert.Equal("/api/wms/topology/location-types", handler.RequestPath);
        Assert.Equal("?includeInactive=true", handler.RequestQuery);
    }

    [Fact]
    public async Task TryCreateWarehouseAsync_WhenSuccessful_PostsSharedRequestAndMapsDetails()
    {
        WarehouseDetails details = CreateWarehouseDetails();
        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson(details),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsTopologyApiClient apiClient = new(httpClient);

        ApiResult<WarehouseDetails> result = await apiClient.TryCreateWarehouseAsync(
            new CreateWarehouseRequest(" WH-A ", "Warehouse A", "North"),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(details, result.Value);
        Assert.Equal(HttpMethod.Post, handler.RequestMethod);
        Assert.Equal("/api/wms/topology/warehouses", handler.RequestPath);
        using JsonDocument body = JsonDocument.Parse(handler.RequestBody);
        Assert.Equal(" WH-A ", body.RootElement.GetProperty("code").GetString());
        Assert.Equal("Warehouse A", body.RootElement.GetProperty("name").GetString());
        Assert.Equal("North", body.RootElement.GetProperty("description").GetString());
    }

    [Fact]
    public async Task TryUpdateZoneDetailsAsync_WhenSuccessful_PutsSharedRequestAndMapsDetails()
    {
        ZoneDetails details = CreateZoneDetails(name: "Updated Zone");
        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson(details),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsTopologyApiClient apiClient = new(httpClient);

        ApiResult<ZoneDetails> result = await apiClient.TryUpdateZoneDetailsAsync(
            details.Id,
            new UpdateZoneDetailsRequest("Updated Zone", null),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(details, result.Value);
        Assert.Equal(HttpMethod.Put, handler.RequestMethod);
        Assert.Equal($"/api/wms/topology/zones/{details.Id}", handler.RequestPath);
        using JsonDocument body = JsonDocument.Parse(handler.RequestBody);
        Assert.Equal("Updated Zone", body.RootElement.GetProperty("name").GetString());
        Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("description").ValueKind);
    }

    [Fact]
    public async Task TryDeactivateWarehouseAsync_WhenSuccessful_PostsLifecycleRoute()
    {
        WarehouseDetails details = CreateWarehouseDetails(isActive: false);
        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson(details),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsTopologyApiClient apiClient = new(httpClient);

        ApiResult<WarehouseDetails> result = await apiClient.TryDeactivateWarehouseAsync(
            details.Id,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsActive);
        Assert.Equal(HttpMethod.Post, handler.RequestMethod);
        Assert.Equal($"/api/wms/topology/warehouses/{details.Id}/deactivate", handler.RequestPath);
    }

    [Fact]
    public async Task TryReactivateZoneAsync_WhenSuccessful_PostsLifecycleRoute()
    {
        ZoneDetails details = CreateZoneDetails(isActive: true);
        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson(details),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsTopologyApiClient apiClient = new(httpClient);

        ApiResult<ZoneDetails> result = await apiClient.TryReactivateZoneAsync(
            details.Id,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsActive);
        Assert.Equal(HttpMethod.Post, handler.RequestMethod);
        Assert.Equal($"/api/wms/topology/zones/{details.Id}/reactivate", handler.RequestPath);
    }

    [Fact]
    public async Task TryDeactivateStorageLocationAsync_WhenSuccessful_PostsLifecycleRoute()
    {
        StorageLocationDetails details = CreateStorageLocationDetails(isActive: false);
        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson(details),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsTopologyApiClient apiClient = new(httpClient);

        ApiResult<StorageLocationDetails> result = await apiClient.TryDeactivateStorageLocationAsync(
            details.Id,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsActive);
        Assert.Equal(HttpMethod.Post, handler.RequestMethod);
        Assert.Equal($"/api/wms/topology/locations/{details.Id}/deactivate", handler.RequestPath);
    }

    [Fact]
    public async Task TryCreateWarehouseAsync_WhenProblemDetailsReturned_ReturnsParsedApiError()
    {
        using StubHttpMessageHandler handler = new(
            HttpStatusCode.Conflict,
            """
            {
              "status": 409,
              "title": "Conflict",
              "detail": "Warehouse code already exists.",
              "code": "Warehouse.CodeConflict"
            }
            """,
            "application/problem+json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsTopologyApiClient apiClient = new(httpClient);

        ApiResult<WarehouseDetails> result = await apiClient.TryCreateWarehouseAsync(
            new CreateWarehouseRequest("WH-A", "Warehouse A", null),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(409, result.Error!.Status);
        Assert.Equal("Warehouse code already exists.", result.Error.Message);
        Assert.Equal("Warehouse.CodeConflict", result.Error.Extensions["code"]);
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

    private static WarehouseDetails CreateWarehouseDetails(bool isActive = true)
    {
        return new WarehouseDetails(
            Guid.Parse("018f0000-0000-7000-8000-000000000101"),
            "WH-A",
            "Warehouse A",
            "North",
            isActive,
            DateTimeOffset.Parse("2026-06-17T09:00:00Z"),
            null);
    }

    private static ZoneDetails CreateZoneDetails(string name = "Zone A", bool isActive = true)
    {
        return new ZoneDetails(
            Guid.Parse("018f0000-0000-7000-8000-000000000201"),
            Guid.Parse("018f0000-0000-7000-8000-000000000101"),
            "ZONE-A",
            name,
            null,
            isActive,
            DateTimeOffset.Parse("2026-06-17T09:00:00Z"),
            null);
    }

    private static StorageLocationDetails CreateStorageLocationDetails(bool isActive = true)
    {
        return new StorageLocationDetails(
            Guid.Parse("018f0000-0000-7000-8000-000000000501"),
            Guid.Parse("018f0000-0000-7000-8000-000000000101"),
            Guid.Parse("018f0000-0000-7000-8000-000000000201"),
            Guid.Parse("018f0000-0000-7000-8000-000000000301"),
            Guid.Parse("018f0000-0000-7000-8000-000000000401"),
            "A-01",
            "A 01",
            null,
            true,
            isActive,
            DateTimeOffset.Parse("2026-06-17T09:00:00Z"),
            null);
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
