using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.AppDispatching.QueryDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Inventory.Endpoints;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryTransfers;
using Myrmex.Modules.Wms.Inventory.Features.InventoryTransfers;
using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Inventory;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Myrmex.Tests.Wms.Inventory.Endpoints;

public sealed class InventoryTransferEndpointTests
{
    [Fact]
    public async Task CreateInventoryTransferAsync_BindsRequestAndSerializesDetails()
    {
        Guid warehouseId = Guid.Parse("018f0000-0000-7000-8000-000000000301");
        Guid stockKeepingUnitId = Guid.Parse("018f0000-0000-7000-8000-000000000101");
        Guid sourceLocationId = Guid.Parse("018f0000-0000-7000-8000-000000000201");
        Guid destinationLocationId = Guid.Parse("018f0000-0000-7000-8000-000000000202");
        InventoryTransferDetails details = CreateInventoryTransferDetails(
            warehouseId,
            stockKeepingUnitId,
            sourceLocationId,
            destinationLocationId);
        RecordingCommandDispatcher commandDispatcher = new(details);
        await using WebApplication app = CreateInventoryEndpointApp(commandDispatcher);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await app.StartAsync(cancellationToken);

        try
        {
            using HttpClient httpClient = CreateHttpClient(app);
            CreateInventoryTransferRequest request = new(
                warehouseId,
                warehouseId,
                TransitStorageLocationId: null,
                [
                    new CreateInventoryTransferLineRequest(
                        stockKeepingUnitId,
                        sourceLocationId,
                        destinationLocationId,
                        RequestedQuantity: 5)
                ]);

            using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
                "/api/wms/inventory/transfers",
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();
            Assert.NotNull(commandDispatcher.CapturedCommand);
            Assert.Equal(warehouseId, commandDispatcher.CapturedCommand.SourceWarehouseId);
            Assert.Equal(warehouseId, commandDispatcher.CapturedCommand.DestinationWarehouseId);
            Assert.Null(commandDispatcher.CapturedCommand.TransitStorageLocationId);

            CreateInventoryTransfer.Line line = Assert.Single(commandDispatcher.CapturedCommand.Lines);
            Assert.Equal(stockKeepingUnitId, line.StockKeepingUnitId);
            Assert.Equal(sourceLocationId, line.SourceStorageLocationId);
            Assert.Equal(destinationLocationId, line.DestinationStorageLocationId);
            Assert.Equal(5, line.RequestedQuantity);

            using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            JsonElement root = json.RootElement;
            Assert.Equal(details.Id, root.GetProperty("id").GetGuid());
            Assert.Equal(InventoryTransferStatusDetails.Created, root.GetProperty("status").GetString());
            Assert.Equal(1, root.GetProperty("lines").GetArrayLength());
        }
        finally
        {
            await app.StopAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task CreateInventoryTransferAsync_WhenValidationFails_Returns400WithProblemDetails()
    {
        RecordingCommandDispatcher commandDispatcher = new(
            ServiceResult<InventoryTransferDetails>.Fail(ServiceError.Validation<InventoryTransferDetails>(
                "Transfer line quantity is invalid.",
                "Lines[0].RequestedQuantity")));
        await using WebApplication app = CreateInventoryEndpointApp(commandDispatcher);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await app.StartAsync(cancellationToken);

        try
        {
            using HttpClient httpClient = CreateHttpClient(app);
            CreateInventoryTransferRequest request = new(
                Guid.Parse("018f0000-0000-7000-8000-000000000301"),
                Guid.Parse("018f0000-0000-7000-8000-000000000301"),
                TransitStorageLocationId: null,
                []);

            using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
                "/api/wms/inventory/transfers",
                request,
                cancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            Assert.Equal("Validation-InventoryTransferDetails-Lines[0].RequestedQuantity", json.RootElement.GetProperty("code").GetString());
        }
        finally
        {
            await app.StopAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task MoveInventoryTransferLineAsync_BindsRouteAndRequestBodyAndSerializesDetails()
    {
        Guid warehouseId = Guid.Parse("018f0000-0000-7000-8000-000000000301");
        Guid stockKeepingUnitId = Guid.Parse("018f0000-0000-7000-8000-000000000101");
        Guid sourceLocationId = Guid.Parse("018f0000-0000-7000-8000-000000000201");
        Guid destinationLocationId = Guid.Parse("018f0000-0000-7000-8000-000000000202");
        InventoryTransferDetails details = CreateInventoryTransferDetails(
            warehouseId,
            stockKeepingUnitId,
            sourceLocationId,
            destinationLocationId,
            status: InventoryTransferStatusDetails.InProgress);
        InventoryTransferLineDetails line = Assert.Single(details.Lines);
        RecordingCommandDispatcher commandDispatcher = new(details);
        await using WebApplication app = CreateInventoryEndpointApp(commandDispatcher);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await app.StartAsync(cancellationToken);

        try
        {
            using HttpClient httpClient = CreateHttpClient(app);

            using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
                $"/api/wms/inventory/transfers/{details.Id}/lines/{line.Id}/move",
                new MoveInventoryTransferLineRequest(3),
                cancellationToken);

            response.EnsureSuccessStatusCode();
            Assert.NotNull(commandDispatcher.CapturedMoveCommand);
            Assert.Equal(details.Id, commandDispatcher.CapturedMoveCommand.TransferId);
            Assert.Equal(line.Id, commandDispatcher.CapturedMoveCommand.LineId);
            Assert.Equal(3, commandDispatcher.CapturedMoveCommand.Quantity);

            using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            JsonElement root = json.RootElement;
            Assert.Equal(details.Id, root.GetProperty("id").GetGuid());
            Assert.Equal(InventoryTransferStatusDetails.InProgress, root.GetProperty("status").GetString());
            Assert.Equal(1, root.GetProperty("lines").GetArrayLength());
        }
        finally
        {
            await app.StopAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task MoveInventoryTransferLineAsync_WhenConflictFails_Returns409WithProblemDetails()
    {
        RecordingCommandDispatcher commandDispatcher = new(
            ServiceResult<InventoryTransferDetails>.Fail(MoveInventoryTransferLine.OverMoveConflict()));
        await using WebApplication app = CreateInventoryEndpointApp(commandDispatcher);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await app.StartAsync(cancellationToken);

        try
        {
            using HttpClient httpClient = CreateHttpClient(app);
            Guid transferId = Guid.Parse("018f0000-0000-7000-8000-000000000001");
            Guid lineId = Guid.Parse("018f0000-0000-7000-8000-000000000401");

            using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
                $"/api/wms/inventory/transfers/{transferId}/lines/{lineId}/move",
                new MoveInventoryTransferLineRequest(6),
                cancellationToken);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

            using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            Assert.Equal("InventoryTransfer.OverMove", json.RootElement.GetProperty("code").GetString());
        }
        finally
        {
            await app.StopAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task ListInventoryTransfersAsync_BindsQueryParametersAndSerializesListItems()
    {
        Guid warehouseId = Guid.Parse("018f0000-0000-7000-8000-000000000301");
        Guid stockKeepingUnitId = Guid.Parse("018f0000-0000-7000-8000-000000000101");
        Guid sourceLocationId = Guid.Parse("018f0000-0000-7000-8000-000000000201");
        Guid destinationLocationId = Guid.Parse("018f0000-0000-7000-8000-000000000202");
        Guid transitLocationId = Guid.Parse("018f0000-0000-7000-8000-000000000203");
        InventoryTransferListItem listItem = CreateInventoryTransferListItem(
            warehouseId,
            transitLocationId);
        RecordingQueryDispatcher queryDispatcher = new(listItem);
        await using WebApplication app = CreateInventoryEndpointApp(new RecordingCommandDispatcher(
            CreateInventoryTransferDetails(warehouseId, stockKeepingUnitId, sourceLocationId, destinationLocationId)),
            queryDispatcher);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await app.StartAsync(cancellationToken);

        try
        {
            using HttpClient httpClient = CreateHttpClient(app);

            using HttpResponseMessage response = await httpClient.GetAsync(
                $"/api/wms/inventory/transfers?skip=7&take=13&sortBy={InventoryTransferSortBy.TotalInTransitQuantity}&sortDescending=false&warehouseId={warehouseId}&status=InProgress&createdFromUtc=2026-06-18T09%3A00%3A00.0000000%2B00%3A00&createdToUtc=2026-06-19T09%3A00%3A00.0000000%2B00%3A00&transferCode=TR&sourceStorageLocationId={sourceLocationId}&destinationStorageLocationId={destinationLocationId}&stockKeepingUnitId={stockKeepingUnitId}&hasTransitLocation=true",
                cancellationToken);

            response.EnsureSuccessStatusCode();
            Assert.NotNull(queryDispatcher.CapturedListQuery);
            Assert.Equal(7, queryDispatcher.CapturedListQuery.Skip);
            Assert.Equal(13, queryDispatcher.CapturedListQuery.Take);
            Assert.Equal(InventoryTransferSortBy.TotalInTransitQuantity, queryDispatcher.CapturedListQuery.SortBy);
            Assert.False(queryDispatcher.CapturedListQuery.SortDescending);
            Assert.Equal(warehouseId, queryDispatcher.CapturedListQuery.WarehouseId);
            Assert.Equal("InProgress", queryDispatcher.CapturedListQuery.StatusText);
            Assert.Equal(InventoryTransferStatus.InProgress, queryDispatcher.CapturedListQuery.Status);
            Assert.Equal(DateTimeOffset.Parse("2026-06-18T09:00:00.0000000+00:00"), queryDispatcher.CapturedListQuery.CreatedFromUtc);
            Assert.Equal(DateTimeOffset.Parse("2026-06-19T09:00:00.0000000+00:00"), queryDispatcher.CapturedListQuery.CreatedToUtc);
            Assert.Equal("TR", queryDispatcher.CapturedListQuery.TransferCode);
            Assert.Equal(sourceLocationId, queryDispatcher.CapturedListQuery.SourceStorageLocationId);
            Assert.Equal(destinationLocationId, queryDispatcher.CapturedListQuery.DestinationStorageLocationId);
            Assert.Equal(stockKeepingUnitId, queryDispatcher.CapturedListQuery.StockKeepingUnitId);
            Assert.True(queryDispatcher.CapturedListQuery.HasTransitLocation);

            using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            JsonElement root = json.RootElement;
            JsonElement item = root.GetProperty("items")[0];

            Assert.Equal(1, root.GetProperty("totalCount").GetInt32());
            Assert.Equal(7, root.GetProperty("skip").GetInt32());
            Assert.Equal(13, root.GetProperty("take").GetInt32());
            Assert.Equal(listItem.Id, item.GetProperty("id").GetGuid());
            Assert.Equal(listItem.Code, item.GetProperty("code").GetString());
            Assert.Equal(4, item.GetProperty("totalPickedQuantity").GetDecimal());
            Assert.Equal(2, item.GetProperty("totalPlacedQuantity").GetDecimal());
            Assert.Equal(2, item.GetProperty("totalInTransitQuantity").GetDecimal());
            Assert.Equal("MAIN", item.GetProperty("sourceWarehouse").GetProperty("code").GetString());
            Assert.Equal("TR-IN-01", item.GetProperty("transitStorageLocation").GetProperty("code").GetString());
        }
        finally
        {
            await app.StopAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task GetInventoryTransferByIdAsync_WhenTransferExists_SerializesDetailsAndMovementHistory()
    {
        Guid warehouseId = Guid.Parse("018f0000-0000-7000-8000-000000000301");
        Guid stockKeepingUnitId = Guid.Parse("018f0000-0000-7000-8000-000000000101");
        Guid sourceLocationId = Guid.Parse("018f0000-0000-7000-8000-000000000201");
        Guid destinationLocationId = Guid.Parse("018f0000-0000-7000-8000-000000000202");
        InventoryTransferDetails details = CreateInventoryTransferDetails(
            warehouseId,
            stockKeepingUnitId,
            sourceLocationId,
            destinationLocationId,
            status: InventoryTransferStatusDetails.InProgress,
            includeMovement: true);
        RecordingQueryDispatcher queryDispatcher = new(ServiceResult<InventoryTransferDetails>.Success(details));
        await using WebApplication app = CreateInventoryEndpointApp(new RecordingCommandDispatcher(details), queryDispatcher);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await app.StartAsync(cancellationToken);

        try
        {
            using HttpClient httpClient = CreateHttpClient(app);

            using HttpResponseMessage response = await httpClient.GetAsync(
                $"/api/wms/inventory/transfers/{details.Id}",
                cancellationToken);

            response.EnsureSuccessStatusCode();
            Assert.NotNull(queryDispatcher.CapturedDetailsQuery);
            Assert.Equal(details.Id, queryDispatcher.CapturedDetailsQuery.TransferId);

            using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            JsonElement root = json.RootElement;
            JsonElement line = root.GetProperty("lines")[0];
            JsonElement movement = root.GetProperty("movements")[0];

            Assert.Equal(details.Id, root.GetProperty("id").GetGuid());
            Assert.Equal(details.Status, root.GetProperty("status").GetString());
            Assert.Equal(5, line.GetProperty("remainingToPickQuantity").GetDecimal());
            Assert.Equal(0, line.GetProperty("remainingToPlaceQuantity").GetDecimal());
            Assert.Equal("Direct", movement.GetProperty("movementMeaning").GetString());
            Assert.Equal(stockKeepingUnitId, movement.GetProperty("sku").GetProperty("id").GetGuid());
            Assert.Equal(sourceLocationId, movement.GetProperty("fromStorageLocation").GetProperty("id").GetGuid());
            Assert.Equal(destinationLocationId, movement.GetProperty("toStorageLocation").GetProperty("id").GetGuid());
        }
        finally
        {
            await app.StopAsync(cancellationToken);
        }
    }

    private static WebApplication CreateInventoryEndpointApp(
        RecordingCommandDispatcher commandDispatcher,
        RecordingQueryDispatcher? queryDispatcher = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton<ICommandDispatcher>(commandDispatcher);
        builder.Services.AddSingleton<IQueryDispatcher>(queryDispatcher ?? new RecordingQueryDispatcher());
        builder.Services.AddTestAuthentication();

        WebApplication app = builder.Build();
        app.UseTestAuthentication();
        app.MapGroup("/api/wms/inventory")
            .RequireAuthorization(Myrmex.AspNetCore.Security.MyrmexAuthorizationPolicies.WmsOperator)
            .MapInventoryTransferEndpoints();

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

    private static InventoryTransferDetails CreateInventoryTransferDetails(
        Guid warehouseId,
        Guid stockKeepingUnitId,
        Guid sourceLocationId,
        Guid destinationLocationId,
        string status = InventoryTransferStatusDetails.Created,
        bool includeMovement = false)
    {
        Guid lineId = Guid.Parse("018f0000-0000-7000-8000-000000000401");

        return new InventoryTransferDetails(
            Guid.Parse("018f0000-0000-7000-8000-000000000001"),
            "TR-001",
            status,
            DateTimeOffset.Parse("2026-06-19T09:00:00Z"),
            UpdatedAtUtc: null,
            new InventoryTransferDetails.WarehouseInfo(warehouseId, "MAIN", "Main Warehouse"),
            new InventoryTransferDetails.WarehouseInfo(warehouseId, "MAIN", "Main Warehouse"),
            TransitStorageLocation: null,
            [
                new InventoryTransferLineDetails(
                    lineId,
                    RequestedQuantity: 5,
                    MovedQuantity: 0,
                    PickedQuantity: 0,
                    PlacedQuantity: 0,
                    InTransitQuantity: 0,
                    RemainingToPickQuantity: 5,
                    RemainingToPlaceQuantity: 0,
                    new InventoryTransferLineDetails.StockKeepingUnitInfo(
                        stockKeepingUnitId,
                        "SKU-001",
                        "Widget",
                        new InventoryTransferLineDetails.UnitOfMeasureInfo(
                            Guid.Parse("018f0000-0000-7000-8000-000000000111"),
                            "EA",
                            "ea")),
                    new InventoryTransferLineDetails.StorageLocationInfo(
                        sourceLocationId,
                        "A-01-01",
                        "A-01-01"),
                    new InventoryTransferLineDetails.StorageLocationInfo(
                        destinationLocationId,
                        "A-01-02",
                        "A-01-02"))
            ],
            includeMovement
                ?
                [
                    new InventoryTransferMovementDetails(
                        Guid.Parse("018f0000-0000-7000-8000-000000000501"),
                        lineId,
                        Guid.Parse("018f0000-0000-7000-8000-000000000601"),
                        DateTimeOffset.Parse("2026-06-19T09:15:00Z"),
                        3,
                        "Direct",
                        new InventoryTransferMovementDetails.StockKeepingUnitInfo(
                            stockKeepingUnitId,
                            "SKU-001",
                            "Widget",
                            new InventoryTransferMovementDetails.UnitOfMeasureInfo(
                                Guid.Parse("018f0000-0000-7000-8000-000000000111"),
                                "EA",
                                "ea")),
                        new InventoryTransferMovementDetails.StorageLocationInfo(
                            sourceLocationId,
                            "A-01-01",
                            "A-01-01"),
                        new InventoryTransferMovementDetails.StorageLocationInfo(
                            destinationLocationId,
                            "A-01-02",
                            "A-01-02"))
                ]
                : []);
    }

    private static InventoryTransferListItem CreateInventoryTransferListItem(
        Guid warehouseId,
        Guid transitLocationId)
    {
        return new InventoryTransferListItem(
            Guid.Parse("018f0000-0000-7000-8000-000000000701"),
            "TR-001",
            InventoryTransferStatusDetails.InProgress,
            DateTimeOffset.Parse("2026-06-19T09:00:00Z"),
            UpdatedAtUtc: null,
            TotalRequestedQuantity: 5,
            TotalPickedQuantity: 4,
            TotalPlacedQuantity: 2,
            TotalInTransitQuantity: 2,
            new InventoryTransferListItem.WarehouseInfo(warehouseId, "MAIN", "Main Warehouse"),
            new InventoryTransferListItem.WarehouseInfo(warehouseId, "MAIN", "Main Warehouse"),
            new InventoryTransferListItem.StorageLocationInfo(transitLocationId, "TR-IN-01", "TR-IN-01"));
    }

    private sealed class RecordingCommandDispatcher : ICommandDispatcher
    {
        private readonly ServiceResult<InventoryTransferDetails> _result;

        public RecordingCommandDispatcher(InventoryTransferDetails details)
            : this(ServiceResult<InventoryTransferDetails>.Success(details))
        {
        }

        public RecordingCommandDispatcher(ServiceResult<InventoryTransferDetails> result)
        {
            _result = result;
        }

        public CreateInventoryTransfer.Command? CapturedCommand { get; private set; }

        public MoveInventoryTransferLine.Command? CapturedMoveCommand { get; private set; }

        public Task<TResult> DispatchAsync<TCommand, TResult>(
            TCommand command,
            CancellationToken cancellationToken = default)
            where TCommand : ICommand<TResult>
            where TResult : IServiceResult
        {
            if (command is CreateInventoryTransfer.Command createCommand &&
                typeof(TResult) == typeof(ServiceResult<InventoryTransferDetails>))
            {
                CapturedCommand = createCommand;

                return Task.FromResult((TResult)(object)_result);
            }

            if (command is MoveInventoryTransferLine.Command moveCommand &&
                typeof(TResult) == typeof(ServiceResult<InventoryTransferDetails>))
            {
                CapturedMoveCommand = moveCommand;

                return Task.FromResult((TResult)(object)_result);
            }

            throw new NotSupportedException($"Unexpected command type {typeof(TCommand).FullName}.");
        }
    }

    private sealed class RecordingQueryDispatcher : IQueryDispatcher
    {
        private readonly InventoryTransferListItem? _listItem;
        private readonly ServiceResult<InventoryTransferDetails>? _detailsResult;

        public RecordingQueryDispatcher()
        {
        }

        public RecordingQueryDispatcher(InventoryTransferListItem listItem)
        {
            _listItem = listItem;
        }

        public RecordingQueryDispatcher(ServiceResult<InventoryTransferDetails> detailsResult)
        {
            _detailsResult = detailsResult;
        }

        public ListInventoryTransfers.Query? CapturedListQuery { get; private set; }

        public GetInventoryTransferById.Query? CapturedDetailsQuery { get; private set; }

        public Task<TResult> DispatchAsync<TQuery, TResult>(
            TQuery query,
            CancellationToken cancellationToken = default)
            where TQuery : IQuery<TResult>
            where TResult : IServiceResult
        {
            if (query is ListInventoryTransfers.Query listQuery &&
                typeof(TResult) == typeof(ServiceResult<ListResult<InventoryTransferListItem>>))
            {
                CapturedListQuery = listQuery;

                ServiceResult<ListResult<InventoryTransferListItem>> result =
                    ServiceResult<ListResult<InventoryTransferListItem>>.Success(
                        new ListResult<InventoryTransferListItem>(
                            [_listItem!],
                            TotalCount: 1,
                            listQuery.Skip,
                            listQuery.Take));

                return Task.FromResult((TResult)(object)result);
            }

            if (query is GetInventoryTransferById.Query detailsQuery &&
                typeof(TResult) == typeof(ServiceResult<InventoryTransferDetails>))
            {
                CapturedDetailsQuery = detailsQuery;

                return Task.FromResult((TResult)(object)_detailsResult!);
            }

            throw new NotSupportedException($"Unexpected query type {typeof(TQuery).FullName}.");
        }
    }
}
