using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.AppDispatching.QueryDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Inventory.Endpoints;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryTransactions;
using Myrmex.Modules.Wms.Inventory.Features.InventoryLedger;
using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Inventory;
using System.Text.Json;

namespace Myrmex.Tests.Wms.Inventory.Endpoints;

public sealed class InventoryLedgerEndpointTests
{
    [Fact]
    public async Task ListInventoryLedgerEntriesAsync_BindsQueryParametersAndSerializesNestedDetails()
    {
        Guid entryId = Guid.Parse("018f0000-0000-7000-8000-000000000501");
        Guid transactionId = Guid.Parse("018f0000-0000-7000-8000-000000000401");
        Guid stockKeepingUnitId = Guid.Parse("018f0000-0000-7000-8000-000000000101");
        Guid warehouseId = Guid.Parse("018f0000-0000-7000-8000-000000000301");
        Guid storageLocationId = Guid.Parse("018f0000-0000-7000-8000-000000000201");
        InventoryLedgerEntryDetails details = CreateInventoryLedgerEntryDetails(
            entryId,
            transactionId,
            stockKeepingUnitId,
            warehouseId,
            storageLocationId);
        RecordingQueryDispatcher queryDispatcher = new(details);
        await using WebApplication app = CreateInventoryEndpointApp(queryDispatcher);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await app.StartAsync(cancellationToken);

        try
        {
            using HttpClient httpClient = CreateHttpClient(app);

            using HttpResponseMessage response = await httpClient.GetAsync(
                $"/api/wms/inventory/ledger?skip=7&take=13&sortBy={InventoryLedgerSortBy.WarehouseCode}&sortDescending=true&stockKeepingUnitId={stockKeepingUnitId}&warehouseId={warehouseId}&storageLocationId={storageLocationId}&transactionType=Adjustment&occurredFromUtc=2026-06-18T09%3A00%3A00.0000000%2B00%3A00&occurredToUtc=2026-06-19T09%3A00%3A00.0000000%2B00%3A00",
                cancellationToken);

            response.EnsureSuccessStatusCode();
            Assert.NotNull(queryDispatcher.CapturedListQuery);
            Assert.Equal(7, queryDispatcher.CapturedListQuery.Skip);
            Assert.Equal(13, queryDispatcher.CapturedListQuery.Take);
            Assert.Equal(InventoryLedgerSortBy.WarehouseCode, queryDispatcher.CapturedListQuery.SortBy);
            Assert.True(queryDispatcher.CapturedListQuery.SortDescending);
            Assert.Equal(stockKeepingUnitId, queryDispatcher.CapturedListQuery.StockKeepingUnitId);
            Assert.Equal(warehouseId, queryDispatcher.CapturedListQuery.WarehouseId);
            Assert.Equal(storageLocationId, queryDispatcher.CapturedListQuery.StorageLocationId);
            Assert.Equal("Adjustment", queryDispatcher.CapturedListQuery.TransactionType);
            Assert.Equal(DateTimeOffset.Parse("2026-06-18T09:00:00.0000000+00:00"), queryDispatcher.CapturedListQuery.OccurredFromUtc);
            Assert.Equal(DateTimeOffset.Parse("2026-06-19T09:00:00.0000000+00:00"), queryDispatcher.CapturedListQuery.OccurredToUtc);

            using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            JsonElement root = json.RootElement;
            JsonElement item = root.GetProperty("items")[0];

            Assert.Equal(1, root.GetProperty("totalCount").GetInt32());
            Assert.Equal(7, root.GetProperty("skip").GetInt32());
            Assert.Equal(13, root.GetProperty("take").GetInt32());
            Assert.Equal(details.EntryId, item.GetProperty("entryId").GetGuid());
            Assert.Equal(details.TransactionId, item.GetProperty("transactionId").GetGuid());
            Assert.Equal(details.TransactionType, item.GetProperty("transactionType").GetString());
            Assert.Equal(details.Reason, item.GetProperty("reason").GetString());
            Assert.Equal(details.QuantityDelta, item.GetProperty("quantityDelta").GetDecimal());
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

    [Fact]
    public async Task GetInventoryTransactionByIdAsync_WhenTransactionExists_SerializesHeaderAndNestedEntries()
    {
        Guid transactionId = Guid.Parse("018f0000-0000-7000-8000-000000000401");
        InventoryTransactionDetails details = CreateInventoryTransactionDetails(transactionId);
        RecordingQueryDispatcher queryDispatcher = new(
            ServiceResult<InventoryTransactionDetails>.Success(details));
        await using WebApplication app = CreateInventoryEndpointApp(queryDispatcher);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await app.StartAsync(cancellationToken);

        try
        {
            using HttpClient httpClient = CreateHttpClient(app);

            using HttpResponseMessage response = await httpClient.GetAsync(
                $"/api/wms/inventory/transactions/{transactionId}",
                cancellationToken);

            response.EnsureSuccessStatusCode();
            Assert.NotNull(queryDispatcher.CapturedTransactionQuery);
            Assert.Equal(transactionId, queryDispatcher.CapturedTransactionQuery.TransactionId);

            using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            JsonElement root = json.RootElement;
            JsonElement entry = root.GetProperty("entries")[0];

            Assert.Equal(details.Id, root.GetProperty("id").GetGuid());
            Assert.Equal(details.TransactionType, root.GetProperty("transactionType").GetString());
            Assert.Equal(details.Reason, root.GetProperty("reason").GetString());
            Assert.Equal(details.CreatedAtUtc, root.GetProperty("createdAtUtc").GetDateTimeOffset());
            Assert.Equal(details.Entries[0].EntryId, entry.GetProperty("entryId").GetGuid());
            Assert.Equal(details.Entries[0].QuantityDelta, entry.GetProperty("quantityDelta").GetDecimal());
            Assert.Equal(details.Entries[0].Sku.Code, entry.GetProperty("sku").GetProperty("code").GetString());
            Assert.Equal(
                details.Entries[0].StorageLocation.Warehouse.Code,
                entry.GetProperty("storageLocation").GetProperty("warehouse").GetProperty("code").GetString());
            Assert.False(entry.TryGetProperty("transactionId", out _));
            Assert.False(entry.TryGetProperty("transactionType", out _));
        }
        finally
        {
            await app.StopAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task GetInventoryTransactionByIdAsync_WhenTransactionMissing_ReturnsNotFound()
    {
        Guid transactionId = Guid.Parse("018f0000-0000-7000-8000-000000000999");
        RecordingQueryDispatcher queryDispatcher = new(
            ServiceResult<InventoryTransactionDetails>.Fail(ServiceError.NotFound<InventoryTransaction>()));
        await using WebApplication app = CreateInventoryEndpointApp(queryDispatcher);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await app.StartAsync(cancellationToken);

        try
        {
            using HttpClient httpClient = CreateHttpClient(app);

            using HttpResponseMessage response = await httpClient.GetAsync(
                $"/api/wms/inventory/transactions/{transactionId}",
                cancellationToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.NotNull(queryDispatcher.CapturedTransactionQuery);
            Assert.Equal(transactionId, queryDispatcher.CapturedTransactionQuery.TransactionId);
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
        app.MapGroup("/api/wms/inventory").MapInventoryLedgerEndpoints();

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

    private static InventoryLedgerEntryDetails CreateInventoryLedgerEntryDetails(
        Guid entryId,
        Guid transactionId,
        Guid stockKeepingUnitId,
        Guid warehouseId,
        Guid storageLocationId)
    {
        return new InventoryLedgerEntryDetails(
            entryId,
            transactionId,
            "Adjustment",
            "Cycle count correction",
            DateTimeOffset.Parse("2026-06-18T09:30:00+00:00"),
            BalanceBefore: 10,
            QuantityDelta: -3,
            BalanceAfter: 7,
            new InventoryLedgerEntryDetails.StockKeepingUnitInfo(
                stockKeepingUnitId,
                "SKU-001",
                "Widget",
                new InventoryLedgerEntryDetails.UnitOfMeasureInfo(
                    Guid.Parse("018f0000-0000-7000-8000-000000000111"),
                    "EA",
                    "ea")),
            new InventoryLedgerEntryDetails.StorageLocationInfo(
                storageLocationId,
                "A-01-01",
                "A-01-01",
                new InventoryLedgerEntryDetails.WarehouseInfo(
                warehouseId,
                "MAIN",
                "Main Warehouse")));
    }

    private static InventoryTransactionDetails CreateInventoryTransactionDetails(Guid transactionId)
    {
        return new InventoryTransactionDetails(
            transactionId,
            "Adjustment",
            "Cycle count correction",
            DateTimeOffset.Parse("2026-06-18T09:30:00+00:00"),
            DateTimeOffset.Parse("2026-06-18T09:31:00+00:00"),
            [
                new InventoryTransactionEntryDetails(
                    Guid.Parse("018f0000-0000-7000-8000-000000000501"),
                    BalanceBefore: 10,
                    QuantityDelta: -3,
                    BalanceAfter: 7,
                    new InventoryTransactionEntryDetails.StockKeepingUnitInfo(
                        Guid.Parse("018f0000-0000-7000-8000-000000000101"),
                        "SKU-001",
                        "Widget",
                        new InventoryTransactionEntryDetails.UnitOfMeasureInfo(
                            Guid.Parse("018f0000-0000-7000-8000-000000000111"),
                            "EA",
                            "ea")),
                    new InventoryTransactionEntryDetails.StorageLocationInfo(
                        Guid.Parse("018f0000-0000-7000-8000-000000000201"),
                        "A-01-01",
                        "A-01-01",
                        new InventoryTransactionEntryDetails.WarehouseInfo(
                            Guid.Parse("018f0000-0000-7000-8000-000000000301"),
                            "MAIN",
                            "Main Warehouse")))
            ]);
    }

    private sealed class RecordingQueryDispatcher : IQueryDispatcher
    {
        private readonly InventoryLedgerEntryDetails? ledgerEntryDetails;
        private readonly ServiceResult<InventoryTransactionDetails>? transactionDetailsResult;

        public RecordingQueryDispatcher(InventoryLedgerEntryDetails details)
        {
            ledgerEntryDetails = details;
        }

        public RecordingQueryDispatcher(ServiceResult<InventoryTransactionDetails> result)
        {
            transactionDetailsResult = result;
        }

        public ListInventoryLedgerEntries.Query? CapturedListQuery { get; private set; }

        public GetInventoryTransactionById.Query? CapturedTransactionQuery { get; private set; }

        public Task<TResult> DispatchAsync<TQuery, TResult>(
            TQuery query,
            CancellationToken cancellationToken = default)
            where TQuery : IQuery<TResult>
            where TResult : IServiceResult
        {
            if (query is ListInventoryLedgerEntries.Query listQuery &&
                typeof(TResult) == typeof(ServiceResult<ListResult<InventoryLedgerEntryDetails>>))
            {
                CapturedListQuery = listQuery;

                ServiceResult<ListResult<InventoryLedgerEntryDetails>> result =
                    ServiceResult<ListResult<InventoryLedgerEntryDetails>>.Success(
                        new ListResult<InventoryLedgerEntryDetails>(
                            [ledgerEntryDetails!],
                            TotalCount: 1,
                            listQuery.Skip,
                            listQuery.Take));

                return Task.FromResult((TResult)(object)result);
            }

            if (query is GetInventoryTransactionById.Query transactionQuery &&
                typeof(TResult) == typeof(ServiceResult<InventoryTransactionDetails>))
            {
                CapturedTransactionQuery = transactionQuery;

                return Task.FromResult((TResult)(object)transactionDetailsResult!);
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
