using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Inventory.Endpoints;
using Myrmex.Modules.Wms.Inventory.Features.InventoryTransfers;
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

    private static WebApplication CreateInventoryEndpointApp(RecordingCommandDispatcher commandDispatcher)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton<ICommandDispatcher>(commandDispatcher);

        WebApplication app = builder.Build();
        app.MapGroup("/api/wms/inventory").MapInventoryTransferEndpoints();

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
        Guid destinationLocationId)
    {
        return new InventoryTransferDetails(
            Guid.Parse("018f0000-0000-7000-8000-000000000001"),
            "TR-001",
            InventoryTransferStatusDetails.Created,
            DateTimeOffset.Parse("2026-06-19T09:00:00Z"),
            UpdatedAtUtc: null,
            new InventoryTransferDetails.WarehouseInfo(warehouseId, "MAIN", "Main Warehouse"),
            new InventoryTransferDetails.WarehouseInfo(warehouseId, "MAIN", "Main Warehouse"),
            TransitStorageLocation: null,
            [
                new InventoryTransferLineDetails(
                    Guid.Parse("018f0000-0000-7000-8000-000000000401"),
                    RequestedQuantity: 5,
                    MovedQuantity: 0,
                    PickedQuantity: 0,
                    PlacedQuantity: 0,
                    InTransitQuantity: 0,
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
            []);
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

            throw new NotSupportedException($"Unexpected command type {typeof(TCommand).FullName}.");
        }
    }
}
