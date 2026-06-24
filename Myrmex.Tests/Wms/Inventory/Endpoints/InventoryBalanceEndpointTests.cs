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
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Myrmex.Tests.Wms.Inventory.Endpoints;

public sealed class InventoryBalanceEndpointTests
{
    [Fact]
    public async Task MoveInventoryBalanceAsync_BindsBodyAndSerializesResult()
    {
        Guid stockKeepingUnitId = Guid.Parse("018f0000-0000-7000-8000-000000000101");
        Guid sourceStorageLocationId = Guid.Parse("018f0000-0000-7000-8000-000000000201");
        Guid destinationStorageLocationId = Guid.Parse("018f0000-0000-7000-8000-000000000202");
        MoveInventoryBalanceResult moveResult = CreateMoveResult(
            stockKeepingUnitId,
            sourceStorageLocationId,
            destinationStorageLocationId);
        RecordingCommandDispatcher commandDispatcher = new(moveResult);
        await using WebApplication app = CreateInventoryEndpointApp(
            new RecordingQueryDispatcher(moveResult.SourceBalance),
            commandDispatcher);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await app.StartAsync(cancellationToken);

        try
        {
            using HttpClient httpClient = CreateHttpClient(app);
            MoveInventoryBalanceRequest request = new(
                stockKeepingUnitId,
                sourceStorageLocationId,
                destinationStorageLocationId,
                Quantity: 4,
                Reason: "Consolidate stock",
                ExpectedSourceBalanceVersion: "AAAAAAAAB9E=");

            using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
                "/api/wms/inventory/balances/move",
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();
            Assert.NotNull(commandDispatcher.CapturedMoveCommand);
            Assert.Equal(stockKeepingUnitId, commandDispatcher.CapturedMoveCommand.StockKeepingUnitId);
            Assert.Equal(sourceStorageLocationId, commandDispatcher.CapturedMoveCommand.SourceStorageLocationId);
            Assert.Equal(destinationStorageLocationId, commandDispatcher.CapturedMoveCommand.DestinationStorageLocationId);
            Assert.Equal(4, commandDispatcher.CapturedMoveCommand.Quantity);
            Assert.Equal("Consolidate stock", commandDispatcher.CapturedMoveCommand.Reason);
            Assert.Equal("AAAAAAAAB9E=", commandDispatcher.CapturedMoveCommand.ExpectedSourceBalanceVersion);
            Assert.True(commandDispatcher.CapturedCancellationToken.CanBeCanceled);

            using JsonDocument json = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(cancellationToken));
            Assert.Equal(4, json.RootElement.GetProperty("movedQuantity").GetDecimal());
            Assert.Equal(6, json.RootElement.GetProperty("sourceQuantityAfter").GetDecimal());
            Assert.Equal(7, json.RootElement.GetProperty("destinationQuantityAfter").GetDecimal());
        }
        finally
        {
            await app.StopAsync(cancellationToken);
        }
    }

    [Theory]
    [InlineData(ServiceErrorType.Invalid, HttpStatusCode.BadRequest)]
    [InlineData(ServiceErrorType.NotFound, HttpStatusCode.NotFound)]
    [InlineData(ServiceErrorType.Conflict, HttpStatusCode.Conflict)]
    public async Task MoveInventoryBalanceAsync_WhenCommandFails_ReturnsProblemDetails(
        ServiceErrorType errorType,
        HttpStatusCode expectedStatus)
    {
        ServiceError error = new(
            errorType,
            "InventoryBalance.MoveRejected",
            "Move rejected.",
            "Quantity");
        RecordingCommandDispatcher commandDispatcher = new(
            ServiceResult<MoveInventoryBalanceResult>.Fail(error));
        InventoryBalanceDetails details = CreateInventoryBalanceDetails(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());
        await using WebApplication app = CreateInventoryEndpointApp(
            new RecordingQueryDispatcher(details),
            commandDispatcher);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await app.StartAsync(cancellationToken);

        try
        {
            using HttpClient httpClient = CreateHttpClient(app);
            using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
                "/api/wms/inventory/balances/move",
                new MoveInventoryBalanceRequest(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    1,
                    "Move",
                    "AAAAAAAAB9E="),
                cancellationToken);

            Assert.Equal(expectedStatus, response.StatusCode);
            using JsonDocument json = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(cancellationToken));
            Assert.Equal(
                "InventoryBalance.MoveRejected",
                json.RootElement.GetProperty("code").GetString());
        }
        finally
        {
            await app.StopAsync(cancellationToken);
        }
    }

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

    private static WebApplication CreateInventoryEndpointApp(
        RecordingQueryDispatcher queryDispatcher,
        ICommandDispatcher? commandDispatcher = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton<IQueryDispatcher>(queryDispatcher);
        builder.Services.AddSingleton<ICommandDispatcher>(
            commandDispatcher ?? new UnsupportedCommandDispatcher());

        WebApplication app = builder.Build();
        app.MapGroup("/api/wms/inventory").MapInventoryBalanceEndpoints();

        return app;
    }

    private static MoveInventoryBalanceResult CreateMoveResult(
        Guid stockKeepingUnitId,
        Guid sourceStorageLocationId,
        Guid destinationStorageLocationId)
    {
        Guid warehouseId = Guid.Parse("018f0000-0000-7000-8000-000000000301");
        InventoryBalanceDetails source = CreateInventoryBalanceDetails(
            stockKeepingUnitId,
            sourceStorageLocationId,
            warehouseId) with
        {
            Quantity = 6,
            BalanceVersion = "AAAAAAAAB9I="
        };
        InventoryBalanceDetails destination = CreateInventoryBalanceDetails(
            stockKeepingUnitId,
            destinationStorageLocationId,
            warehouseId) with
        {
            Quantity = 7,
            BalanceVersion = "AAAAAAAAB9M="
        };

        return new MoveInventoryBalanceResult(
            source,
            destination,
            MovedQuantity: 4,
            SourceQuantityBefore: 10,
            SourceQuantityAfter: 6,
            DestinationQuantityBefore: 3,
            DestinationQuantityAfter: 7,
            DateTimeOffset.Parse("2026-06-24T09:00:00Z"));
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
            "AAAAAAAAB9E=",
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

    private sealed class RecordingCommandDispatcher : ICommandDispatcher
    {
        private readonly ServiceResult<MoveInventoryBalanceResult> _result;

        public RecordingCommandDispatcher(MoveInventoryBalanceResult result)
            : this(ServiceResult<MoveInventoryBalanceResult>.Success(result))
        {
        }

        public RecordingCommandDispatcher(ServiceResult<MoveInventoryBalanceResult> result)
        {
            _result = result;
        }

        public MoveInventoryBalance.Command? CapturedMoveCommand { get; private set; }

        public CancellationToken CapturedCancellationToken { get; private set; }

        public Task<TResult> DispatchAsync<TCommand, TResult>(
            TCommand command,
            CancellationToken cancellationToken = default)
            where TCommand : ICommand<TResult>
            where TResult : IServiceResult
        {
            if (command is MoveInventoryBalance.Command moveCommand &&
                typeof(TResult) == typeof(ServiceResult<MoveInventoryBalanceResult>))
            {
                CapturedMoveCommand = moveCommand;
                CapturedCancellationToken = cancellationToken;
                return Task.FromResult((TResult)(object)_result);
            }

            throw new NotSupportedException(
                $"Unexpected command type {typeof(TCommand).FullName}.");
        }
    }
}
