using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Topology;
using Myrmex.WebApp.Wms.Api;
using Myrmex.WebApp.Wms.Topology;
using System.Text;
using System.Text.Json;

namespace Myrmex.Tests.Wms.Topology.Client;

public sealed class WmsTopologyApiClientTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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
            new ListRequest(
                Skip: 5,
                Take: 10,
                SearchText: "zone",
                SortBy: "code",
                IncludeInactive: true),
            TestContext.Current.CancellationToken);

        ZoneDetails item = Assert.Single(result.Items);
        Assert.Equal(details.Id, item.Id);
        Assert.Equal(warehouseId, item.WarehouseId);
        Assert.Equal("ZONE-A", item.Code);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(HttpMethod.Get, handler.RequestMethod);
        Assert.Equal($"/api/wms/topology/warehouses/{warehouseId}/zones", handler.RequestPath);
        Assert.Equal("?skip=5&take=10&searchText=zone&sortBy=code&sortDescending=false&includeInactive=true", handler.RequestQuery);
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
                SelectableOnly = false
            },
            cancellationTokenSource.Token);

        StorageLocationLookupItem item = Assert.Single(result);
        Assert.Equal(details.Id, item.Id);
        Assert.Equal(warehouseId, item.WarehouseId);
        Assert.False(item.IsActive);
        Assert.Equal(HttpMethod.Get, handler.RequestMethod);
        Assert.Equal($"/api/wms/topology/warehouses/{warehouseId}/locations/lookup", handler.RequestPath);
        Assert.Equal("?searchText=Pick&take=20&selectableOnly=false", handler.RequestQuery);
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
