using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Myrmex.AppDispatching.QueryDispatching;
using Myrmex.AspNetCore.Results;
using Myrmex.Core.Application.Queries;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Inventory.Features.InventoryLedger;
using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Inventory;

namespace Myrmex.Modules.Wms.Inventory.Endpoints;

internal static class InventoryLedgerEndpoints
{
    public const string LedgerRoute = "/ledger";
    public const string TransactionsRoute = "/transactions/{transactionId:guid}";

    public static RouteGroupBuilder MapInventoryLedgerEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet(LedgerRoute, ListInventoryLedgerEntriesAsync)
            .WithName("ListInventoryLedgerEntries")
            .WithSummary("List Inventory Ledger Entries");

        group.MapGet(TransactionsRoute, GetInventoryTransactionByIdAsync)
            .WithName("GetInventoryTransactionById")
            .WithSummary("Get Inventory Transaction By Id");

        return group;
    }

    private static async Task<IResult> ListInventoryLedgerEntriesAsync(
        [AsParameters] ListInventoryLedgerEntriesRequest request,
        IQueryDispatcher queryDispatcher,
        CancellationToken cancellationToken = default)
    {
        var query = new ListInventoryLedgerEntries.Query
        {
            Skip = request.Skip ?? 0,
            Take = request.Take ?? ListQuery.DefaultTake,
            SortBy = request.SortBy,
            SortDescending = request.SortDescending ?? false,
            StockKeepingUnitId = request.StockKeepingUnitId,
            WarehouseId = request.WarehouseId,
            StorageLocationId = request.StorageLocationId,
            TransactionType = request.TransactionType,
            OccurredFromUtc = request.OccurredFromUtc,
            OccurredToUtc = request.OccurredToUtc
        };

        var result = await queryDispatcher
            .DispatchAsync<ListInventoryLedgerEntries.Query, ServiceResult<ListResult<InventoryLedgerEntryDetails>>>(
                query,
                cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> GetInventoryTransactionByIdAsync(
        Guid transactionId,
        IQueryDispatcher queryDispatcher,
        CancellationToken cancellationToken = default)
    {
        var query = new GetInventoryTransactionById.Query(transactionId);

        var result = await queryDispatcher
            .DispatchAsync<GetInventoryTransactionById.Query, ServiceResult<InventoryTransactionDetails>>(
                query,
                cancellationToken);

        return result.ToHttpResult();
    }
}
