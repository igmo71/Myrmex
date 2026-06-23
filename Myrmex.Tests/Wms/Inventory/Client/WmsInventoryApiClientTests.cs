using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Inventory;
using Myrmex.WebApp.Wms.Api;
using Myrmex.WebApp.Wms.Inventory;
using System.Text;
using System.Text.Json;
using System.Web;

namespace Myrmex.Tests.Wms.Inventory.Client;

public sealed class WmsInventoryApiClientTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task ListInventoryBalancesAsync_WhenRequestHasNoValues_OmitsNullableQueryParametersAndMapsNestedDetails()
    {
        InventoryBalanceDetails details = CreateInventoryBalanceDetails();
        ListResult<InventoryBalanceDetails> response = new([details], TotalCount: 1, Skip: 0, Take: 20);
        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson(response),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsInventoryApiClient apiClient = new(httpClient);

        ListResult<InventoryBalanceDetails> result = await apiClient.ListInventoryBalancesAsync(
            new ListInventoryBalancesRequest(),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Get, handler.RequestMethod);
        Assert.Equal("/api/wms/inventory/balances", handler.RequestPath);
        Assert.Equal(string.Empty, handler.RequestQuery);

        InventoryBalanceDetails item = Assert.Single(result.Items);
        Assert.Equal(details.Id, item.Id);
        Assert.Equal(details.Sku.Id, item.Sku.Id);
        Assert.Equal(details.Sku.Code, item.Sku.Code);
        Assert.Equal(details.Sku.BaseUom.Id, item.Sku.BaseUom.Id);
        Assert.Equal(details.Sku.BaseUom.Symbol, item.Sku.BaseUom.Symbol);
        Assert.Equal(details.StorageLocation.Id, item.StorageLocation.Id);
        Assert.Equal(details.StorageLocation.Warehouse.Id, item.StorageLocation.Warehouse.Id);
        Assert.Equal(details.StorageLocation.Warehouse.Code, item.StorageLocation.Warehouse.Code);
    }

    [Fact]
    public async Task ListInventoryBalancesAsync_WhenRequestHasExplicitValues_IncludesQueryParametersAndPropagatesCancellation()
    {
        Guid stockKeepingUnitId = Guid.Parse("018f0000-0000-7000-8000-000000000101");
        Guid storageLocationId = Guid.Parse("018f0000-0000-7000-8000-000000000201");
        Guid warehouseId = Guid.Parse("018f0000-0000-7000-8000-000000000301");
        ListResult<InventoryBalanceDetails> response = new([], TotalCount: 0, Skip: 0, Take: 50);
        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson(response),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsInventoryApiClient apiClient = new(httpClient);
        using CancellationTokenSource cancellationTokenSource = new();

        await apiClient.ListInventoryBalancesAsync(
            new ListInventoryBalancesRequest
            {
                Skip = 0,
                Take = 50,
                SortBy = InventoryBalanceSortBy.SkuCode,
                SortDescending = false,
                StockKeepingUnitId = stockKeepingUnitId,
                StorageLocationId = storageLocationId,
                WarehouseId = warehouseId
            },
            cancellationTokenSource.Token);

        Dictionary<string, string> query = ParseQuery(handler.RequestQuery);

        Assert.Equal("0", query["skip"]);
        Assert.Equal("50", query["take"]);
        Assert.Equal(InventoryBalanceSortBy.SkuCode, query["sortBy"]);
        Assert.Equal("false", query["sortDescending"]);
        Assert.Equal(stockKeepingUnitId.ToString(), query["stockKeepingUnitId"]);
        Assert.Equal(storageLocationId.ToString(), query["storageLocationId"]);
        Assert.Equal(warehouseId.ToString(), query["warehouseId"]);
    }

    [Fact]
    public async Task ListInventoryLedgerEntriesAsync_WhenRequestHasNoValues_OmitsNullableQueryParametersAndMapsNestedDetails()
    {
        InventoryLedgerEntryDetails details = CreateInventoryLedgerEntryDetails();
        ListResult<InventoryLedgerEntryDetails> response = new([details], TotalCount: 1, Skip: 0, Take: 20);
        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson(response),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsInventoryApiClient apiClient = new(httpClient);

        ListResult<InventoryLedgerEntryDetails> result = await apiClient.ListInventoryLedgerEntriesAsync(
            new ListInventoryLedgerEntriesRequest(),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Get, handler.RequestMethod);
        Assert.Equal("/api/wms/inventory/ledger", handler.RequestPath);
        Assert.Equal(string.Empty, handler.RequestQuery);

        InventoryLedgerEntryDetails item = Assert.Single(result.Items);
        Assert.Equal(details.EntryId, item.EntryId);
        Assert.Equal(details.TransactionId, item.TransactionId);
        Assert.Equal(details.TransactionType, item.TransactionType);
        Assert.Equal(details.Sku.Id, item.Sku.Id);
        Assert.Equal(details.Sku.BaseUom.Symbol, item.Sku.BaseUom.Symbol);
        Assert.Equal(details.StorageLocation.Id, item.StorageLocation.Id);
        Assert.Equal(details.StorageLocation.Warehouse.Id, item.StorageLocation.Warehouse.Id);
        Assert.Equal(details.StorageLocation.Warehouse.Code, item.StorageLocation.Warehouse.Code);
    }

    [Fact]
    public async Task ListInventoryLedgerEntriesAsync_WhenRequestHasExplicitValues_IncludesQueryParameters()
    {
        Guid stockKeepingUnitId = Guid.Parse("018f0000-0000-7000-8000-000000000101");
        Guid warehouseId = Guid.Parse("018f0000-0000-7000-8000-000000000301");
        Guid storageLocationId = Guid.Parse("018f0000-0000-7000-8000-000000000201");
        DateTimeOffset occurredFromUtc = DateTimeOffset.Parse("2026-06-18T09:00:00+00:00");
        DateTimeOffset occurredToUtc = DateTimeOffset.Parse("2026-06-19T09:00:00+00:00");
        ListResult<InventoryLedgerEntryDetails> response = new([], TotalCount: 0, Skip: 7, Take: 13);
        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson(response),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsInventoryApiClient apiClient = new(httpClient);

        await apiClient.ListInventoryLedgerEntriesAsync(
            new ListInventoryLedgerEntriesRequest
            {
                Skip = 7,
                Take = 13,
                SortBy = InventoryLedgerSortBy.WarehouseCode,
                SortDescending = true,
                StockKeepingUnitId = stockKeepingUnitId,
                WarehouseId = warehouseId,
                StorageLocationId = storageLocationId,
                TransactionType = "Adjustment",
                OccurredFromUtc = occurredFromUtc,
                OccurredToUtc = occurredToUtc
            },
            TestContext.Current.CancellationToken);

        Dictionary<string, string> query = ParseQuery(handler.RequestQuery);

        Assert.Equal("7", query["skip"]);
        Assert.Equal("13", query["take"]);
        Assert.Equal(InventoryLedgerSortBy.WarehouseCode, query["sortBy"]);
        Assert.Equal("true", query["sortDescending"]);
        Assert.Equal(stockKeepingUnitId.ToString(), query["stockKeepingUnitId"]);
        Assert.Equal(warehouseId.ToString(), query["warehouseId"]);
        Assert.Equal(storageLocationId.ToString(), query["storageLocationId"]);
        Assert.Equal("Adjustment", query["transactionType"]);
        Assert.Equal(occurredFromUtc.ToString("O"), query["occurredFromUtc"]);
        Assert.Equal(occurredToUtc.ToString("O"), query["occurredToUtc"]);
    }

    [Fact]
    public async Task ListInventoryTransfersAsync_WhenRequestHasExplicitValues_IncludesQueryParametersAndMapsListItems()
    {
        Guid warehouseId = Guid.Parse("018f0000-0000-7000-8000-000000000301");
        Guid stockKeepingUnitId = Guid.Parse("018f0000-0000-7000-8000-000000000101");
        Guid sourceLocationId = Guid.Parse("018f0000-0000-7000-8000-000000000201");
        Guid destinationLocationId = Guid.Parse("018f0000-0000-7000-8000-000000000202");
        DateTimeOffset createdFromUtc = DateTimeOffset.Parse("2026-06-18T09:00:00+00:00");
        DateTimeOffset createdToUtc = DateTimeOffset.Parse("2026-06-19T09:00:00+00:00");
        InventoryTransferListItem details = CreateInventoryTransferListItem(warehouseId);
        ListResult<InventoryTransferListItem> response = new([details], TotalCount: 1, Skip: 7, Take: 13);
        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson(response),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsInventoryApiClient apiClient = new(httpClient);

        ListResult<InventoryTransferListItem> result = await apiClient.ListInventoryTransfersAsync(
            new ListInventoryTransfersRequest
            {
                Skip = 7,
                Take = 13,
                SortBy = InventoryTransferSortBy.TotalInTransitQuantity,
                SortDescending = true,
                WarehouseId = warehouseId,
                Status = InventoryTransferStatusDetails.InProgress,
                CreatedFromUtc = createdFromUtc,
                CreatedToUtc = createdToUtc,
                TransferCode = "TR",
                SourceStorageLocationId = sourceLocationId,
                DestinationStorageLocationId = destinationLocationId,
                StockKeepingUnitId = stockKeepingUnitId,
                HasTransitLocation = true
            },
            TestContext.Current.CancellationToken);

        Dictionary<string, string> query = ParseQuery(handler.RequestQuery);

        Assert.Equal(HttpMethod.Get, handler.RequestMethod);
        Assert.Equal("/api/wms/inventory/transfers", handler.RequestPath);
        Assert.Equal("7", query["skip"]);
        Assert.Equal("13", query["take"]);
        Assert.Equal(InventoryTransferSortBy.TotalInTransitQuantity, query["sortBy"]);
        Assert.Equal("true", query["sortDescending"]);
        Assert.Equal(warehouseId.ToString(), query["warehouseId"]);
        Assert.Equal(InventoryTransferStatusDetails.InProgress, query["status"]);
        Assert.Equal(createdFromUtc.ToString("O"), query["createdFromUtc"]);
        Assert.Equal(createdToUtc.ToString("O"), query["createdToUtc"]);
        Assert.Equal("TR", query["transferCode"]);
        Assert.Equal(sourceLocationId.ToString(), query["sourceStorageLocationId"]);
        Assert.Equal(destinationLocationId.ToString(), query["destinationStorageLocationId"]);
        Assert.Equal(stockKeepingUnitId.ToString(), query["stockKeepingUnitId"]);
        Assert.Equal("true", query["hasTransitLocation"]);

        InventoryTransferListItem item = Assert.Single(result.Items);
        Assert.Equal(details.Id, item.Id);
        Assert.Equal(4, item.TotalPickedQuantity);
        Assert.Equal(2, item.TotalPlacedQuantity);
        Assert.Equal(2, item.TotalInTransitQuantity);
        Assert.Equal("MAIN", item.SourceWarehouse.Code);
        Assert.Equal("TR-IN-01", item.TransitStorageLocation?.Code);
    }

    [Fact]
    public async Task GetInventoryTransactionByIdAsync_WhenSuccessful_UsesDetailsRouteAndMapsNestedEntries()
    {
        Guid transactionId = Guid.Parse("018f0000-0000-7000-8000-000000000401");
        InventoryTransactionDetails details = CreateInventoryTransactionDetails(transactionId);
        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson(details),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsInventoryApiClient apiClient = new(httpClient);

        InventoryTransactionDetails result = await apiClient.GetInventoryTransactionByIdAsync(
            transactionId,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Get, handler.RequestMethod);
        Assert.Equal($"/api/wms/inventory/transactions/{transactionId}", handler.RequestPath);
        Assert.Equal(string.Empty, handler.RequestQuery);
        Assert.Equal(details.Id, result.Id);
        Assert.Equal(details.TransactionType, result.TransactionType);
        Assert.Equal(details.Reason, result.Reason);
        Assert.Equal(details.CreatedAtUtc, result.CreatedAtUtc);

        InventoryTransactionEntryDetails entry = Assert.Single(result.Entries);
        Assert.Equal(details.Entries[0].EntryId, entry.EntryId);
        Assert.Equal(details.Entries[0].BalanceBefore, entry.BalanceBefore);
        Assert.Equal(details.Entries[0].QuantityDelta, entry.QuantityDelta);
        Assert.Equal(details.Entries[0].BalanceAfter, entry.BalanceAfter);
        Assert.Equal(details.Entries[0].Sku.Id, entry.Sku.Id);
        Assert.Equal(details.Entries[0].Sku.BaseUom.Symbol, entry.Sku.BaseUom.Symbol);
        Assert.Equal(details.Entries[0].StorageLocation.Id, entry.StorageLocation.Id);
        Assert.Equal(details.Entries[0].StorageLocation.Warehouse.Code, entry.StorageLocation.Warehouse.Code);
    }

    [Fact]
    public async Task GetInventoryTransferByIdAsync_WhenSuccessful_UsesDetailsRouteAndMapsMovementHistory()
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
            movedQuantity: 3,
            includeMovement: true);
        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson(details),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsInventoryApiClient apiClient = new(httpClient);

        InventoryTransferDetails result = await apiClient.GetInventoryTransferByIdAsync(
            details.Id,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Get, handler.RequestMethod);
        Assert.Equal($"/api/wms/inventory/transfers/{details.Id}", handler.RequestPath);
        Assert.Equal(details.Id, result.Id);
        InventoryTransferLineDetails line = Assert.Single(result.Lines);
        Assert.Equal(2, line.RemainingToPickQuantity);
        Assert.Equal(0, line.RemainingToPlaceQuantity);
        InventoryTransferMovementDetails movement = Assert.Single(result.Movements);
        Assert.Equal("Direct", movement.MovementMeaning);
        Assert.Equal(stockKeepingUnitId, movement.Sku.Id);
        Assert.Equal(sourceLocationId, movement.FromStorageLocation.Id);
        Assert.Equal(destinationLocationId, movement.ToStorageLocation.Id);
    }

    [Fact]
    public async Task ListInventoryLedgerEntriesAsync_WhenCallerCancels_ObservesCancellableRequest()
    {
        using CancellableHttpMessageHandler handler = new();
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsInventoryApiClient apiClient = new(httpClient);
        using CancellationTokenSource cancellationTokenSource = new();

        Task<ListResult<InventoryLedgerEntryDetails>> requestTask = apiClient.ListInventoryLedgerEntriesAsync(
            new ListInventoryLedgerEntriesRequest(),
            cancellationTokenSource.Token);

        await handler.RequestStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        Assert.True(handler.RequestCancellationToken.CanBeCanceled);

        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => requestTask);
        Assert.True(handler.CancellationObserved);
    }

    [Fact]
    public async Task ListInventoryTransfersAsync_WhenCallerCancels_ObservesCancellableRequest()
    {
        using CancellableHttpMessageHandler handler = new();
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsInventoryApiClient apiClient = new(httpClient);
        using CancellationTokenSource cancellationTokenSource = new();

        Task<ListResult<InventoryTransferListItem>> requestTask = apiClient.ListInventoryTransfersAsync(
            new ListInventoryTransfersRequest(),
            cancellationTokenSource.Token);

        await handler.RequestStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        Assert.True(handler.RequestCancellationToken.CanBeCanceled);

        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => requestTask);
        Assert.True(handler.CancellationObserved);
    }

    [Fact]
    public async Task ListInventoryBalancesAsync_WhenProblemDetailsReturned_ThrowsApiException()
    {
        const string problemJson = """
            {
              "type": "https://httpstatuses.com/500",
              "title": "Internal Server Error",
              "status": 500,
              "detail": "Inventory balances could not be loaded.",
              "code": "InventoryBalance.ListFailed"
            }
            """;

        using HttpClient httpClient = CreateHttpClient(new StubHttpMessageHandler(
            HttpStatusCode.InternalServerError,
            problemJson,
            "application/problem+json"));
        WmsInventoryApiClient apiClient = new(httpClient);

        ApiException exception = await Assert.ThrowsAsync<ApiException>(() =>
            apiClient.ListInventoryBalancesAsync(
                new ListInventoryBalancesRequest(),
                TestContext.Current.CancellationToken));

        Assert.Equal(500, exception.Status);
        Assert.Equal("Inventory balances could not be loaded.", exception.Message);
        Assert.Equal("InventoryBalance.ListFailed", exception.Extensions["code"]);
    }

    [Fact]
    public async Task TryAdjustInventoryBalanceAsync_WhenSuccessful_PostsRequestBodyAndMapsVersion()
    {
        Guid stockKeepingUnitId = Guid.Parse("018f0000-0000-7000-8000-000000000101");
        Guid storageLocationId = Guid.Parse("018f0000-0000-7000-8000-000000000201");
        InventoryBalanceDetails details = CreateInventoryBalanceDetails(
            stockKeepingUnitId: stockKeepingUnitId,
            storageLocationId: storageLocationId,
            quantity: 14,
            balanceVersion: "AAAAAAAAB9I=");
        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson(details),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsInventoryApiClient apiClient = new(httpClient);

        ApiResult<InventoryBalanceDetails> result = await apiClient.TryAdjustInventoryBalanceAsync(
            new AdjustInventoryBalanceRequest(
                stockKeepingUnitId,
                storageLocationId,
                CountedQuantity: 14,
                Reason: "Cycle count correction",
                ExpectedBalanceVersion: "AAAAAAAAB9E="),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(14, result.Value.Quantity);
        Assert.Equal("AAAAAAAAB9I=", result.Value.BalanceVersion);
        Assert.Equal(HttpMethod.Post, handler.RequestMethod);
        Assert.Equal("/api/wms/inventory/adjustments", handler.RequestPath);

        using JsonDocument requestBody = JsonDocument.Parse(handler.RequestBody);
        JsonElement root = requestBody.RootElement;
        Assert.Equal(stockKeepingUnitId, root.GetProperty("stockKeepingUnitId").GetGuid());
        Assert.Equal(storageLocationId, root.GetProperty("storageLocationId").GetGuid());
        Assert.Equal(14, root.GetProperty("countedQuantity").GetDecimal());
        Assert.Equal("Cycle count correction", root.GetProperty("reason").GetString());
        Assert.Equal("AAAAAAAAB9E=", root.GetProperty("expectedBalanceVersion").GetString());
    }

    [Fact]
    public async Task TryCreateInventoryTransferAsync_WhenSuccessful_PostsRequestBodyAndMapsDetails()
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
        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson(details),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsInventoryApiClient apiClient = new(httpClient);

        ApiResult<InventoryTransferDetails> result = await apiClient.TryCreateInventoryTransferAsync(
            new CreateInventoryTransferRequest(
                warehouseId,
                warehouseId,
                TransitStorageLocationId: null,
                [
                    new CreateInventoryTransferLineRequest(
                        stockKeepingUnitId,
                        sourceLocationId,
                        destinationLocationId,
                        RequestedQuantity: 5)
                ]),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(details.Id, result.Value.Id);
        Assert.Equal(InventoryTransferStatusDetails.Created, result.Value.Status);
        InventoryTransferLineDetails line = Assert.Single(result.Value.Lines);
        Assert.Equal(stockKeepingUnitId, line.Sku.Id);
        Assert.Equal(sourceLocationId, line.SourceStorageLocation.Id);
        Assert.Equal(destinationLocationId, line.DestinationStorageLocation.Id);
        Assert.Equal(5, line.RequestedQuantity);
        Assert.Empty(result.Value.Movements);
        Assert.Equal(HttpMethod.Post, handler.RequestMethod);
        Assert.Equal("/api/wms/inventory/transfers", handler.RequestPath);

        using JsonDocument requestBody = JsonDocument.Parse(handler.RequestBody);
        JsonElement root = requestBody.RootElement;
        Assert.Equal(warehouseId, root.GetProperty("sourceWarehouseId").GetGuid());
        Assert.Equal(warehouseId, root.GetProperty("destinationWarehouseId").GetGuid());
        Assert.True(root.GetProperty("transitStorageLocationId").ValueKind is JsonValueKind.Null);
        JsonElement requestLine = root.GetProperty("lines")[0];
        Assert.Equal(stockKeepingUnitId, requestLine.GetProperty("stockKeepingUnitId").GetGuid());
        Assert.Equal(sourceLocationId, requestLine.GetProperty("sourceStorageLocationId").GetGuid());
        Assert.Equal(destinationLocationId, requestLine.GetProperty("destinationStorageLocationId").GetGuid());
        Assert.Equal(5, requestLine.GetProperty("requestedQuantity").GetDecimal());
    }

    [Fact]
    public async Task TryMoveInventoryTransferLineAsync_WhenSuccessful_PostsQuantityBodyAndMapsRefreshedDetails()
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
            movedQuantity: 3,
            includeMovement: true);
        InventoryTransferLineDetails line = Assert.Single(details.Lines);
        using StubHttpMessageHandler handler = new(
            HttpStatusCode.OK,
            SerializeJson(details),
            "application/json");
        using HttpClient httpClient = CreateHttpClient(handler);
        WmsInventoryApiClient apiClient = new(httpClient);

        ApiResult<InventoryTransferDetails> result = await apiClient.TryMoveInventoryTransferLineAsync(
            details.Id,
            line.Id,
            new MoveInventoryTransferLineRequest(3),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(InventoryTransferStatusDetails.InProgress, result.Value.Status);
        Assert.Equal(3, result.Value.Lines[0].MovedQuantity);
        InventoryTransferMovementDetails movement = Assert.Single(result.Value.Movements);
        Assert.Equal(line.Id, movement.LineId);
        Assert.Equal(stockKeepingUnitId, movement.Sku.Id);
        Assert.Equal(HttpMethod.Post, handler.RequestMethod);
        Assert.Equal($"/api/wms/inventory/transfers/{details.Id}/lines/{line.Id}/move", handler.RequestPath);

        using JsonDocument requestBody = JsonDocument.Parse(handler.RequestBody);
        JsonElement root = requestBody.RootElement;
        Assert.Equal(3, root.GetProperty("quantity").GetDecimal());
        Assert.False(root.TryGetProperty("lineId", out _));
    }

    [Fact]
    public async Task TryPickAndPlaceInventoryTransferLineAsync_WhenSuccessful_PostQuantityBodyAndMapRefreshedDetails()
    {
        Guid warehouseId = Guid.Parse("018f0000-0000-7000-8000-000000000301");
        Guid stockKeepingUnitId = Guid.Parse("018f0000-0000-7000-8000-000000000101");
        Guid sourceLocationId = Guid.Parse("018f0000-0000-7000-8000-000000000201");
        Guid destinationLocationId = Guid.Parse("018f0000-0000-7000-8000-000000000202");
        Guid transitLocationId = Guid.Parse("018f0000-0000-7000-8000-000000000203");
        InventoryTransferDetails details = CreateInventoryTransferDetails(
            warehouseId,
            stockKeepingUnitId,
            sourceLocationId,
            destinationLocationId,
            status: InventoryTransferStatusDetails.InProgress,
            pickedQuantity: 4,
            placedQuantity: 2,
            inTransitQuantity: 2,
            transitStorageLocationId: transitLocationId);
        InventoryTransferLineDetails line = Assert.Single(details.Lines);

        using StubHttpMessageHandler pickHandler = new(
            HttpStatusCode.OK,
            SerializeJson(details),
            "application/json");
        using HttpClient pickHttpClient = CreateHttpClient(pickHandler);
        WmsInventoryApiClient pickApiClient = new(pickHttpClient);

        ApiResult<InventoryTransferDetails> pickResult = await pickApiClient.TryPickInventoryTransferLineAsync(
            details.Id,
            line.Id,
            new PickInventoryTransferLineRequest(4),
            TestContext.Current.CancellationToken);

        Assert.True(pickResult.IsSuccess);
        Assert.NotNull(pickResult.Value);
        Assert.Equal(transitLocationId, pickResult.Value.TransitStorageLocation?.Id);
        Assert.Equal(4, pickResult.Value.Lines[0].PickedQuantity);
        Assert.Equal(2, pickResult.Value.Lines[0].InTransitQuantity);
        Assert.Equal(HttpMethod.Post, pickHandler.RequestMethod);
        Assert.Equal($"/api/wms/inventory/transfers/{details.Id}/lines/{line.Id}/pick", pickHandler.RequestPath);

        using JsonDocument pickRequestBody = JsonDocument.Parse(pickHandler.RequestBody);
        Assert.Equal(4, pickRequestBody.RootElement.GetProperty("quantity").GetDecimal());
        Assert.False(pickRequestBody.RootElement.TryGetProperty("lineId", out _));

        using StubHttpMessageHandler placeHandler = new(
            HttpStatusCode.OK,
            SerializeJson(details),
            "application/json");
        using HttpClient placeHttpClient = CreateHttpClient(placeHandler);
        WmsInventoryApiClient placeApiClient = new(placeHttpClient);

        ApiResult<InventoryTransferDetails> placeResult = await placeApiClient.TryPlaceInventoryTransferLineAsync(
            details.Id,
            line.Id,
            new PlaceInventoryTransferLineRequest(2),
            TestContext.Current.CancellationToken);

        Assert.True(placeResult.IsSuccess);
        Assert.NotNull(placeResult.Value);
        Assert.Equal(2, placeResult.Value.Lines[0].PlacedQuantity);
        Assert.Equal(2, placeResult.Value.Lines[0].InTransitQuantity);
        Assert.Equal(HttpMethod.Post, placeHandler.RequestMethod);
        Assert.Equal($"/api/wms/inventory/transfers/{details.Id}/lines/{line.Id}/place", placeHandler.RequestPath);

        using JsonDocument placeRequestBody = JsonDocument.Parse(placeHandler.RequestBody);
        Assert.Equal(2, placeRequestBody.RootElement.GetProperty("quantity").GetDecimal());
        Assert.False(placeRequestBody.RootElement.TryGetProperty("lineId", out _));
    }

    [Fact]
    public async Task GetInventoryBalanceByIdAsync_WhenMalformedErrorReturned_ThrowsApiExceptionWithFallbackMessage()
    {
        Guid inventoryBalanceId = Guid.Parse("018f0000-0000-7000-8000-000000000999");

        using HttpClient httpClient = CreateHttpClient(new StubHttpMessageHandler(
            HttpStatusCode.BadRequest,
            "not valid problem details",
            "application/problem+json"));
        WmsInventoryApiClient apiClient = new(httpClient);

        ApiException exception = await Assert.ThrowsAsync<ApiException>(() =>
            apiClient.GetInventoryBalanceByIdAsync(
                inventoryBalanceId,
                TestContext.Current.CancellationToken));

        Assert.Equal(400, exception.Status);
        Assert.Equal(
            $"API request failed for GET '/api/wms/inventory/balances/{inventoryBalanceId}'. Status code: 400 BadRequest.",
            exception.Message);
        Assert.Empty(exception.Extensions);
    }

    private static HttpClient CreateHttpClient(HttpMessageHandler handler)
    {
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://myrmex.test")
        };
    }

    private static Dictionary<string, string> ParseQuery(string? query)
    {
        var queryString = HttpUtility.ParseQueryString(query ?? string.Empty);

        return queryString
            .AllKeys
            .Where(x => x is not null)
            .ToDictionary(x => x!, x => queryString[x!]!);
    }

    private static string SerializeJson<T>(T value)
    {
        return JsonSerializer.Serialize(value, JsonOptions);
    }

    private static InventoryBalanceDetails CreateInventoryBalanceDetails(
        Guid? inventoryBalanceId = null,
        Guid? stockKeepingUnitId = null,
        Guid? storageLocationId = null,
        decimal quantity = 10,
        string balanceVersion = "AAAAAAAAB9E=")
    {
        return new InventoryBalanceDetails(
            inventoryBalanceId ?? Guid.Parse("018f0000-0000-7000-8000-000000000001"),
            quantity,
            DateTimeOffset.Parse("2026-06-17T09:00:00Z"),
            DateTimeOffset.Parse("2026-06-17T10:00:00Z"),
            balanceVersion,
            new InventoryBalanceDetails.StockKeepingUnitInfo(
                stockKeepingUnitId ?? Guid.Parse("018f0000-0000-7000-8000-000000000101"),
                "SKU-001",
                "Widget",
                new InventoryBalanceDetails.UnitOfMeasureInfo(
                    Guid.Parse("018f0000-0000-7000-8000-000000000111"),
                    "EA",
                    "ea")),
            new InventoryBalanceDetails.StorageLocationInfo(
                storageLocationId ?? Guid.Parse("018f0000-0000-7000-8000-000000000201"),
                "A-01-01",
                "A-01-01",
                new InventoryBalanceDetails.WarehouseInfo(
                    Guid.Parse("018f0000-0000-7000-8000-000000000301"),
                    "MAIN",
                    "Main Warehouse")));
    }

    private static InventoryLedgerEntryDetails CreateInventoryLedgerEntryDetails(
        Guid? entryId = null,
        Guid? transactionId = null,
        Guid? stockKeepingUnitId = null,
        Guid? warehouseId = null,
        Guid? storageLocationId = null)
    {
        return new InventoryLedgerEntryDetails(
            entryId ?? Guid.Parse("018f0000-0000-7000-8000-000000000501"),
            transactionId ?? Guid.Parse("018f0000-0000-7000-8000-000000000401"),
            "Adjustment",
            "Cycle count correction",
            DateTimeOffset.Parse("2026-06-18T09:30:00+00:00"),
            BalanceBefore: 10,
            QuantityDelta: -3,
            BalanceAfter: 7,
            new InventoryLedgerEntryDetails.StockKeepingUnitInfo(
                stockKeepingUnitId ?? Guid.Parse("018f0000-0000-7000-8000-000000000101"),
                "SKU-001",
                "Widget",
                new InventoryLedgerEntryDetails.UnitOfMeasureInfo(
                    Guid.Parse("018f0000-0000-7000-8000-000000000111"),
                    "EA",
                    "ea")),
            new InventoryLedgerEntryDetails.StorageLocationInfo(
                storageLocationId ?? Guid.Parse("018f0000-0000-7000-8000-000000000201"),
                "A-01-01",
                "A-01-01",
                new InventoryLedgerEntryDetails.WarehouseInfo(
                warehouseId ?? Guid.Parse("018f0000-0000-7000-8000-000000000301"),
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

    private static InventoryTransferListItem CreateInventoryTransferListItem(Guid warehouseId)
    {
        Guid transitLocationId = Guid.Parse("018f0000-0000-7000-8000-000000000203");

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

    private static InventoryTransferDetails CreateInventoryTransferDetails(
        Guid warehouseId,
        Guid stockKeepingUnitId,
        Guid sourceLocationId,
        Guid destinationLocationId,
        string status = InventoryTransferStatusDetails.Created,
        decimal movedQuantity = 0,
        decimal? pickedQuantity = null,
        decimal? placedQuantity = null,
        decimal? inTransitQuantity = null,
        bool includeMovement = false,
        Guid? transitStorageLocationId = null)
    {
        Guid lineId = Guid.Parse("018f0000-0000-7000-8000-000000000602");
        decimal resolvedPickedQuantity = pickedQuantity ?? movedQuantity;
        decimal resolvedPlacedQuantity = placedQuantity ?? movedQuantity;
        decimal resolvedInTransitQuantity = inTransitQuantity ?? 0;

        return new InventoryTransferDetails(
            Guid.Parse("018f0000-0000-7000-8000-000000000601"),
            "TR-001",
            status,
            DateTimeOffset.Parse("2026-06-19T09:00:00Z"),
            UpdatedAtUtc: null,
            new InventoryTransferDetails.WarehouseInfo(warehouseId, "MAIN", "Main Warehouse"),
            new InventoryTransferDetails.WarehouseInfo(warehouseId, "MAIN", "Main Warehouse"),
            transitStorageLocationId is null
                ? null
                : new InventoryTransferDetails.StorageLocationInfo(
                    transitStorageLocationId.Value,
                    "TR-IN-01",
                    "TR-IN-01"),
            [
                new InventoryTransferLineDetails(
                    lineId,
                    RequestedQuantity: 5,
                    movedQuantity,
                    resolvedPickedQuantity,
                    resolvedPlacedQuantity,
                    resolvedInTransitQuantity,
                    RemainingToPickQuantity: 5 - resolvedPickedQuantity,
                    RemainingToPlaceQuantity: resolvedPickedQuantity - resolvedPlacedQuantity,
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
                        Guid.Parse("018f0000-0000-7000-8000-000000000603"),
                        lineId,
                        Guid.Parse("018f0000-0000-7000-8000-000000000604"),
                        DateTimeOffset.Parse("2026-06-19T09:15:00Z"),
                        movedQuantity,
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

    private sealed class CancellableHttpMessageHandler : HttpMessageHandler
    {
        public TaskCompletionSource RequestStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public CancellationToken RequestCancellationToken { get; private set; }

        public bool CancellationObserved { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCancellationToken = cancellationToken;
            RequestStarted.SetResult();

            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                CancellationObserved = true;
                throw;
            }

            throw new InvalidOperationException("The cancellable handler should not complete successfully.");
        }
    }
}
