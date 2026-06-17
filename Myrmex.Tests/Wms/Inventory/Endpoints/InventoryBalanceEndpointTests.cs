using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.AppDispatching.QueryDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Inventory.Endpoints;
using Myrmex.Modules.Wms.Inventory.Features.InventoryBalances;
using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Inventory;
using System.Text.Json;

namespace Myrmex.Tests.Wms.Inventory.Endpoints;

public sealed class InventoryBalanceEndpointTests
{
    [Fact]
    public async Task ListInventoryBalancesAsync_BindsQueryParametersAndSerializesNestedDetails()
    {
        Guid stockKeepingUnitId = Guid.Parse("018f0000-0000-7000-8000-000000000101");
        Guid storageLocationId = Guid.Parse("018f0000-0000-7000-8000-000000000201");
        Guid warehouseId = Guid.Parse("018f0000-0000-7000-8000-000000000301");
        InventoryBalanceDetails details = CreateInventoryBalanceDetails(
            stockKeepingUnitId,
            storageLocationId,
            warehouseId);
        RecordingQueryDispatcher queryDispatcher = new(details);
        await using WebApplication app = CreateInventoryEndpointApp(queryDispatcher);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await app.StartAsync(cancellationToken);

        try
        {
            using HttpClient httpClient = CreateHttpClient(app);

            using HttpResponseMessage response = await httpClient.GetAsync(
                $"/api/wms/inventory/balances?skip=7&take=13&sortBy={InventoryBalanceSortBy.WarehouseCode}&sortDescending=true&stockKeepingUnitId={stockKeepingUnitId}&storageLocationId={storageLocationId}&warehouseId={warehouseId}",
                cancellationToken);

            response.EnsureSuccessStatusCode();
            Assert.NotNull(queryDispatcher.CapturedListQuery);
            Assert.Equal(7, queryDispatcher.CapturedListQuery.Skip);
            Assert.Equal(13, queryDispatcher.CapturedListQuery.Take);
            Assert.Equal(InventoryBalanceSortBy.WarehouseCode, queryDispatcher.CapturedListQuery.SortBy);
            Assert.True(queryDispatcher.CapturedListQuery.SortDescending);
            Assert.Equal(stockKeepingUnitId, queryDispatcher.CapturedListQuery.StockKeepingUnitId);
            Assert.Equal(storageLocationId, queryDispatcher.CapturedListQuery.StorageLocationId);
            Assert.Equal(warehouseId, queryDispatcher.CapturedListQuery.WarehouseId);

            using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            JsonElement root = json.RootElement;
            JsonElement item = root.GetProperty("items")[0];

            Assert.Equal(1, root.GetProperty("totalCount").GetInt32());
            Assert.Equal(7, root.GetProperty("skip").GetInt32());
            Assert.Equal(13, root.GetProperty("take").GetInt32());
            Assert.Equal(details.Id, item.GetProperty("id").GetGuid());
            Assert.Equal(details.Quantity, item.GetProperty("quantity").GetDecimal());
            Assert.Equal(details.Sku.Code, item.GetProperty("sku").GetProperty("code").GetString());
            Assert.Equal(details.Sku.BaseUom.Symbol, item.GetProperty("sku").GetProperty("baseUom").GetProperty("symbol").GetString());
            Assert.Equal(details.StorageLocation.Code, item.GetProperty("storageLocation").GetProperty("code").GetString());
            Assert.Equal(
                details.StorageLocation.Warehouse.Code,
                item.GetProperty("storageLocation").GetProperty("warehouse").GetProperty("code").GetString());
        }
        finally
        {
            await app.StopAsync(cancellationToken);
        }
    }

    private static WebApplication CreateInventoryEndpointApp(RecordingQueryDispatcher queryDispatcher)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton<IQueryDispatcher>(queryDispatcher);
        builder.Services.AddSingleton<ICommandDispatcher, UnsupportedCommandDispatcher>();

        WebApplication app = builder.Build();
        app.MapGroup("/api/wms/inventory").MapInventoryBalanceEndpoints();

        return app;
    }

    private static HttpClient CreateHttpClient(WebApplication app)
    {
        string address = app.Services
            .GetRequiredService<IServer>()
            .Features
            .Get<IServerAddressesFeature>()!
            .Addresses
            .Single();

        return new HttpClient
        {
            BaseAddress = new Uri(address)
        };
    }

    private static InventoryBalanceDetails CreateInventoryBalanceDetails(
        Guid stockKeepingUnitId,
        Guid storageLocationId,
        Guid warehouseId)
    {
        return new InventoryBalanceDetails(
            Guid.Parse("018f0000-0000-7000-8000-000000000001"),
            Quantity: 42.25m,
            DateTimeOffset.Parse("2026-06-17T09:00:00Z"),
            DateTimeOffset.Parse("2026-06-17T10:00:00Z"),
            new InventoryBalanceDetails.StockKeepingUnitInfo(
                stockKeepingUnitId,
                "SKU-001",
                "Widget",
                new InventoryBalanceDetails.UnitOfMeasureInfo(
                    Guid.Parse("018f0000-0000-7000-8000-000000000111"),
                    "EA",
                    "ea")),
            new InventoryBalanceDetails.StorageLocationInfo(
                storageLocationId,
                "A-01-01",
                "A-01-01",
                new InventoryBalanceDetails.WarehouseInfo(
                    warehouseId,
                    "MAIN",
                    "Main Warehouse")));
    }

    private sealed class RecordingQueryDispatcher(InventoryBalanceDetails details) : IQueryDispatcher
    {
        public ListInventoryBalances.Query? CapturedListQuery { get; private set; }

        public Task<TResult> DispatchAsync<TQuery, TResult>(
            TQuery query,
            CancellationToken cancellationToken = default)
            where TQuery : IQuery<TResult>
            where TResult : IServiceResult
        {
            if (query is ListInventoryBalances.Query listQuery &&
                typeof(TResult) == typeof(ServiceResult<ListResult<InventoryBalanceDetails>>))
            {
                CapturedListQuery = listQuery;

                ServiceResult<ListResult<InventoryBalanceDetails>> result =
                    ServiceResult<ListResult<InventoryBalanceDetails>>.Success(
                        new ListResult<InventoryBalanceDetails>(
                            [details],
                            TotalCount: 1,
                            listQuery.Skip,
                            listQuery.Take));

                return Task.FromResult((TResult)(object)result);
            }

            throw new NotSupportedException($"Unexpected query type {typeof(TQuery).FullName}.");
        }
    }

    private sealed class UnsupportedCommandDispatcher : ICommandDispatcher
    {
        public Task<TResult> DispatchAsync<TCommand, TResult>(
            TCommand command,
            CancellationToken cancellationToken = default)
            where TCommand : ICommand<TResult>
            where TResult : IServiceResult
        {
            throw new NotSupportedException("Command dispatch is not expected in this endpoint test.");
        }
    }
}
