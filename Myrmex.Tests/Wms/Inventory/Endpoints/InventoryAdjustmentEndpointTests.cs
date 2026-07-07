using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Inventory.Endpoints;
using Myrmex.Modules.Wms.Inventory.Features.InventoryAdjustments;
using Myrmex.Shared.Wms.Inventory;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Myrmex.Tests.Wms.Inventory.Endpoints;

public sealed class InventoryAdjustmentEndpointTests
{
    [Fact]
    public async Task AdjustInventoryBalanceAsync_BindsRequestAndSerializesBalanceVersion()
    {
        Guid stockKeepingUnitId = Guid.Parse("018f0000-0000-7000-8000-000000000101");
        Guid storageLocationId = Guid.Parse("018f0000-0000-7000-8000-000000000201");
        InventoryBalanceDetails details = CreateInventoryBalanceDetails(
            stockKeepingUnitId,
            storageLocationId,
            balanceVersion: "AAAAAAAAB9I=");
        RecordingCommandDispatcher commandDispatcher = new(details);
        await using WebApplication app = CreateInventoryEndpointApp(commandDispatcher);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await app.StartAsync(cancellationToken);

        try
        {
            using HttpClient httpClient = CreateHttpClient(app);
            AdjustInventoryBalanceRequest request = new(
                stockKeepingUnitId,
                storageLocationId,
                CountedQuantity: 14,
                Reason: "Cycle count correction",
                ExpectedBalanceVersion: "AAAAAAAAB9E=");

            using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
                "/api/wms/inventory/adjustments",
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();
            Assert.NotNull(commandDispatcher.CapturedCommand);
            Assert.Equal(stockKeepingUnitId, commandDispatcher.CapturedCommand.StockKeepingUnitId);
            Assert.Equal(storageLocationId, commandDispatcher.CapturedCommand.StorageLocationId);
            Assert.Equal(14, commandDispatcher.CapturedCommand.CountedQuantity);
            Assert.Equal("Cycle count correction", commandDispatcher.CapturedCommand.Reason);
            Assert.Equal("AAAAAAAAB9E=", commandDispatcher.CapturedCommand.ExpectedBalanceVersion);

            using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            JsonElement root = json.RootElement;
            Assert.Equal(details.Id, root.GetProperty("id").GetGuid());
            Assert.Equal("AAAAAAAAB9I=", root.GetProperty("balanceVersion").GetString());
        }
        finally
        {
            await app.StopAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task AdjustInventoryBalanceAsync_WhenConcurrencyConflict_Returns409WithConflictCode()
    {
        RecordingCommandDispatcher commandDispatcher = new(
            ServiceResult<InventoryBalanceDetails>.Fail(AdjustInventoryBalance.ConcurrencyConflictError()));
        await using WebApplication app = CreateInventoryEndpointApp(commandDispatcher);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await app.StartAsync(cancellationToken);

        try
        {
            using HttpClient httpClient = CreateHttpClient(app);
            AdjustInventoryBalanceRequest request = new(
                Guid.Parse("018f0000-0000-7000-8000-000000000101"),
                Guid.Parse("018f0000-0000-7000-8000-000000000201"),
                CountedQuantity: 14,
                Reason: "Cycle count correction",
                ExpectedBalanceVersion: "AAAAAAAAB9E=");

            using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
                "/api/wms/inventory/adjustments",
                request,
                cancellationToken);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

            using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            Assert.Equal("InventoryBalance.ConcurrencyConflict", json.RootElement.GetProperty("code").GetString());
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
        builder.Services.AddTestAuthentication();

        WebApplication app = builder.Build();
        app.UseTestAuthentication();
        app.MapGroup("/api/wms/inventory")
            .RequireAuthorization(Myrmex.AspNetCore.Security.MyrmexAuthorizationPolicies.WmsOperator)
            .MapInventoryAdjustmentEndpoints();

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
        string balanceVersion)
    {
        return new InventoryBalanceDetails(
            Guid.Parse("018f0000-0000-7000-8000-000000000001"),
            Quantity: 14,
            DateTimeOffset.Parse("2026-06-17T09:00:00Z"),
            DateTimeOffset.Parse("2026-06-17T10:00:00Z"),
            balanceVersion,
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
                    Guid.Parse("018f0000-0000-7000-8000-000000000301"),
                    "MAIN",
                    "Main Warehouse")));
    }

    private sealed class RecordingCommandDispatcher : ICommandDispatcher
    {
        private readonly ServiceResult<InventoryBalanceDetails> _result;

        public RecordingCommandDispatcher(InventoryBalanceDetails details)
            : this(ServiceResult<InventoryBalanceDetails>.Success(details))
        {
        }

        public RecordingCommandDispatcher(ServiceResult<InventoryBalanceDetails> result)
        {
            _result = result;
        }

        public AdjustInventoryBalance.Command? CapturedCommand { get; private set; }

        public Task<TResult> DispatchAsync<TCommand, TResult>(
            TCommand command,
            CancellationToken cancellationToken = default)
            where TCommand : ICommand<TResult>
            where TResult : IServiceResult
        {
            if (command is AdjustInventoryBalance.Command adjustCommand &&
                typeof(TResult) == typeof(ServiceResult<InventoryBalanceDetails>))
            {
                CapturedCommand = adjustCommand;

                return Task.FromResult((TResult)(object)_result);
            }

            throw new NotSupportedException($"Unexpected command type {typeof(TCommand).FullName}.");
        }
    }
}
