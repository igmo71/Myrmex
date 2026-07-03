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
