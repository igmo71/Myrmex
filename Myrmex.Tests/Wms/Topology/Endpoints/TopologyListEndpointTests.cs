using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.AppDispatching.QueryDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Topology.Endpoints;
using Myrmex.Modules.Wms.Topology.Features.StorageLocations;
using Myrmex.Modules.Wms.Topology.Features.Warehouses;
using Myrmex.Modules.Wms.Topology.Features.Zones;
using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Topology;
using System.Text.Json;

namespace Myrmex.Tests.Wms.Topology.Endpoints;

public sealed class TopologyListEndpointTests
{
    [Fact]
    public async Task ListWarehousesAsync_BindsFeatureRequestAndSerializesSharedDetails()
    {
        RecordingQueryDispatcher dispatcher = new();
        await using WebApplication app = CreateApp(dispatcher);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await app.StartAsync(cancellationToken);

        try
        {
            using HttpClient client = CreateHttpClient(app);
            using HttpResponseMessage response = await client.GetAsync(
                "/api/wms/topology/warehouses?skip=2&take=11&searchText=north" +
                "&sortBy=UpdatedAtUtc&sortDescending=true&includeInactive=true",
                cancellationToken);

            response.EnsureSuccessStatusCode();
            ListWarehouses.Query query = Assert.IsType<ListWarehouses.Query>(dispatcher.CapturedQuery);
            Assert.Equal(2, query.Skip);
            Assert.Equal(11, query.Take);
            Assert.Equal("north", query.SearchText);
            Assert.Equal(WarehouseSortBy.UpdatedAtUtc, query.SortBy);
            Assert.True(query.SortDescending);
            Assert.True(query.IncludeInactive);
            using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            Assert.Equal("WH-A", json.RootElement.GetProperty("items")[0].GetProperty("code").GetString());
        }
        finally
        {
            await app.StopAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task ListZonesAsync_UsesRouteWarehouseAndBindsFeatureRequest()
    {
        Guid warehouseId = Guid.Parse("018f0000-0000-7000-8000-000000000101");
        RecordingQueryDispatcher dispatcher = new();
        await using WebApplication app = CreateApp(dispatcher);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await app.StartAsync(cancellationToken);

        try
        {
            using HttpClient client = CreateHttpClient(app);
            using HttpResponseMessage response = await client.GetAsync(
                $"/api/wms/topology/warehouses/{warehouseId}/zones?skip=3&take=12" +
                "&searchText=pick&sortBy=IsActive&sortDescending=false&includeInactive=true",
                cancellationToken);

            response.EnsureSuccessStatusCode();
            ListZones.Query query = Assert.IsType<ListZones.Query>(dispatcher.CapturedQuery);
            Assert.Equal(warehouseId, query.WarehouseId);
            Assert.Equal(3, query.Skip);
            Assert.Equal(12, query.Take);
            Assert.Equal("pick", query.SearchText);
            Assert.Equal(ZoneSortBy.IsActive, query.SortBy);
            Assert.False(query.SortDescending);
            Assert.True(query.IncludeInactive);
        }
        finally
        {
            await app.StopAsync(cancellationToken);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ListStorageLocationsAsync_BindsBothNestedRoutesAndFilters(bool byWarehouse)
    {
        Guid warehouseId = Guid.Parse("018f0000-0000-7000-8000-000000000101");
        Guid zoneId = Guid.Parse("018f0000-0000-7000-8000-000000000201");
        Guid typeId = Guid.Parse("018f0000-0000-7000-8000-000000000301");
        Guid statusId = Guid.Parse("018f0000-0000-7000-8000-000000000401");
        RecordingQueryDispatcher dispatcher = new();
        await using WebApplication app = CreateApp(dispatcher);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await app.StartAsync(cancellationToken);

        try
        {
            using HttpClient client = CreateHttpClient(app);
            string path = byWarehouse
                ? $"/api/wms/topology/warehouses/{warehouseId}/locations?zoneId={zoneId}"
                : $"/api/wms/topology/zones/{zoneId}/locations?warehouseId={warehouseId}";
            using HttpResponseMessage response = await client.GetAsync(
                path + $"&storageLocationTypeId={typeId}&storageLocationStatusId={statusId}" +
                "&skip=4&take=15&searchText=A-01&sortBy=IsPickable" +
                "&sortDescending=true&includeInactive=true",
                cancellationToken);

            response.EnsureSuccessStatusCode();
            ListStorageLocations.Query query = Assert.IsType<ListStorageLocations.Query>(dispatcher.CapturedQuery);
            Assert.Equal(warehouseId, query.WarehouseId);
            Assert.Equal(zoneId, query.ZoneId);
            Assert.Equal(typeId, query.StorageLocationTypeId);
            Assert.Equal(statusId, query.StorageLocationStatusId);
            Assert.Equal(4, query.Skip);
            Assert.Equal(15, query.Take);
            Assert.Equal("A-01", query.SearchText);
            Assert.Equal(StorageLocationSortBy.IsPickable, query.SortBy);
            Assert.True(query.SortDescending);
            Assert.True(query.IncludeInactive);
            using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            Assert.Equal(typeId, json.RootElement.GetProperty("items")[0]
                .GetProperty("storageLocationTypeId").GetGuid());
        }
        finally
        {
            await app.StopAsync(cancellationToken);
        }
    }

    private static WebApplication CreateApp(RecordingQueryDispatcher dispatcher)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton<IQueryDispatcher>(dispatcher);
        builder.Services.AddSingleton<ICommandDispatcher, UnsupportedCommandDispatcher>();
        WebApplication app = builder.Build();
        app.MapTopologyEndpoints();
        return app;
    }

    private static HttpClient CreateHttpClient(WebApplication app)
    {
        string address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        return new HttpClient { BaseAddress = new Uri(address) };
    }

    private sealed class RecordingQueryDispatcher : IQueryDispatcher
    {
        public object? CapturedQuery { get; private set; }

        public Task<TResult> DispatchAsync<TQuery, TResult>(
            TQuery query,
            CancellationToken cancellationToken = default)
            where TQuery : IQuery<TResult>
            where TResult : IServiceResult
        {
            CapturedQuery = query;
            object result = query switch
            {
                ListWarehouses.Query value => ServiceResult<ListResult<WarehouseDetails>>.Success(
                    new ListResult<WarehouseDetails>([Warehouse()], 1, value.Skip, value.Take)),
                ListZones.Query value => ServiceResult<ListResult<ZoneDetails>>.Success(
                    new ListResult<ZoneDetails>([Zone(value.WarehouseId)], 1, value.Skip, value.Take)),
                ListStorageLocations.Query value => ServiceResult<ListResult<StorageLocationDetails>>.Success(
                    new ListResult<StorageLocationDetails>(
                        [Location(value.WarehouseId!.Value, value.ZoneId!.Value)],
                        1,
                        value.Skip,
                        value.Take)),
                _ => throw new NotSupportedException($"Unexpected query {typeof(TQuery).FullName}.")
            };
            return Task.FromResult((TResult)result);
        }

        private static WarehouseDetails Warehouse() => new(
            Guid.Parse("018f0000-0000-7000-8000-000000000101"),
            "WH-A", "Warehouse A", null, true,
            DateTimeOffset.Parse("2026-06-17T09:00:00Z"), null);

        private static ZoneDetails Zone(Guid warehouseId) => new(
            Guid.Parse("018f0000-0000-7000-8000-000000000201"),
            warehouseId, "ZONE-A", "Zone A", null, true,
            DateTimeOffset.Parse("2026-06-17T09:00:00Z"), null);

        private static StorageLocationDetails Location(Guid warehouseId, Guid zoneId) => new(
            Guid.Parse("018f0000-0000-7000-8000-000000000501"),
            warehouseId,
            zoneId,
            Guid.Parse("018f0000-0000-7000-8000-000000000301"),
            Guid.Parse("018f0000-0000-7000-8000-000000000401"),
            "A-01", "A 01", null, true, true,
            DateTimeOffset.Parse("2026-06-17T09:00:00Z"), null);
    }

    private sealed class UnsupportedCommandDispatcher : ICommandDispatcher
    {
        public Task<TResult> DispatchAsync<TCommand, TResult>(
            TCommand command,
            CancellationToken cancellationToken = default)
            where TCommand : ICommand<TResult>
            where TResult : IServiceResult
        {
            throw new NotSupportedException("Commands are not used by Topology list endpoint tests.");
        }
    }
}
