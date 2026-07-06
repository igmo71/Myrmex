using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryTransactions;
using Myrmex.Modules.Wms.Inventory.Features.InventoryCounts;
using Myrmex.Shared.Wms.Inventory;
using Myrmex.Tests.Wms.Inventory.Testing;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Inventory.Features.InventoryCounts;

public sealed class InventoryCountLifecycleHandlerTests
{
    [Fact]
    public async Task Complete_WhenAllCurrentLinesApplied_PersistsAuditAndFinalState()
    {
        await using TestWmsDbContext db = await TestWmsDbContext.CreateAsync();
        PreparedLifecycleCount prepared = await PrepareAppliedCountAsync(db);

        ServiceResult<InventoryCountDetails> result =
            await new CompleteInventoryCount.Handler(
                db.DbContext,
                NullLogger<CompleteInventoryCount.Handler>.Instance)
            .HandleAsync(
                new CompleteInventoryCount.Command(
                    prepared.Count.Id,
                    prepared.Count.CountVersion,
                    "supervisor-1"),
                TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(InventoryCountStatusDetails.Completed, result.Value.Status);
        Assert.Equal("supervisor-1", result.Value.CompletedByActorId);
        Assert.NotNull(result.Value.CompletedAtUtc);
        Assert.Null(result.Value.CancelledByActorId);
    }

    [Fact]
    public async Task Complete_WhenEmptyUnresolvedOrStale_ReturnsConflict()
    {
        await using TestWmsDbContext db = await TestWmsDbContext.CreateAsync();
        SeededInventoryCountReferences references =
            await InventoryCountTestData.SeedReferencesAsync(db.DbContext);
        InventoryCountDetails empty = (await new CreateInventoryCount.Handler(db.DbContext)
            .HandleAsync(
                new CreateInventoryCount.Command(
                    references.Warehouse.Id,
                    null,
                    InventoryCountTestData.ActorId),
                TestContext.Current.CancellationToken)).Value;
        var handler = new CompleteInventoryCount.Handler(
            db.DbContext,
            NullLogger<CompleteInventoryCount.Handler>.Instance);

        ServiceResult<InventoryCountDetails> emptyResult = await handler.HandleAsync(
            new CompleteInventoryCount.Command(
                empty.Id,
                empty.CountVersion,
                InventoryCountTestData.ActorId),
            TestContext.Current.CancellationToken);

        InventoryCountDetails added = (await new AddInventoryCountLine.Handler(db.DbContext)
            .HandleAsync(
                new AddInventoryCountLine.Command(
                    empty.Id,
                    references.StockKeepingUnit.Id,
                    references.ExistingBalanceLocation.Id,
                    empty.CountVersion,
                    InventoryCountTestData.ActorId),
                TestContext.Current.CancellationToken)).Value;
        ServiceResult<InventoryCountDetails> unresolved = await handler.HandleAsync(
            new CompleteInventoryCount.Command(
                added.Id,
                added.CountVersion,
                InventoryCountTestData.ActorId),
            TestContext.Current.CancellationToken);
        ServiceResult<InventoryCountDetails> stale = await handler.HandleAsync(
            new CompleteInventoryCount.Command(
                added.Id,
                empty.CountVersion,
                InventoryCountTestData.ActorId),
            TestContext.Current.CancellationToken);
        ServiceResult<InventoryCountDetails> staleCancel =
            await new CancelInventoryCount.Handler(
                db.DbContext,
                NullLogger<CancelInventoryCount.Handler>.Instance)
            .HandleAsync(
                new CancelInventoryCount.Command(
                    added.Id,
                    empty.CountVersion,
                    InventoryCountTestData.ActorId),
                TestContext.Current.CancellationToken);

        Assert.Equal(ServiceErrorType.Conflict, emptyResult.Error?.Type);
        Assert.Equal(ServiceErrorType.Conflict, unresolved.Error?.Type);
        Assert.Equal(ServiceErrorType.Conflict, stale.Error?.Type);
        Assert.Equal(ServiceErrorType.Conflict, staleCancel.Error?.Type);
    }

    [Fact]
    public async Task Cancel_AfterAppliedLine_PreservesInventoryAndTransaction()
    {
        await using TestWmsDbContext db = await TestWmsDbContext.CreateAsync();
        PreparedLifecycleCount prepared = await PrepareAppliedCountAsync(db);
        decimal balanceQuantity = prepared.Balance.Quantity;
        Guid transactionId = prepared.Transaction.Id;
        int ledgerCount = await db.DbContext.InventoryLedgerEntries.CountAsync(
            TestContext.Current.CancellationToken);

        ServiceResult<InventoryCountDetails> result =
            await new CancelInventoryCount.Handler(
                db.DbContext,
                NullLogger<CancelInventoryCount.Handler>.Instance)
            .HandleAsync(
                new CancelInventoryCount.Command(
                    prepared.Count.Id,
                    prepared.Count.CountVersion,
                    "supervisor-1"),
                TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(InventoryCountStatusDetails.Cancelled, result.Value.Status);
        Assert.Equal("supervisor-1", result.Value.CancelledByActorId);
        Assert.NotNull(result.Value.CancelledAtUtc);
        Assert.Equal(balanceQuantity, prepared.Balance.Quantity);
        Assert.Equal(
            transactionId,
            (await db.DbContext.InventoryTransactions.SingleAsync(
                TestContext.Current.CancellationToken)).Id);
        Assert.Equal(
            ledgerCount,
            await db.DbContext.InventoryLedgerEntries.CountAsync(
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CompleteOrCancel_WhenAlreadyFinal_ReturnsConflict()
    {
        await using TestWmsDbContext db = await TestWmsDbContext.CreateAsync();
        PreparedLifecycleCount prepared = await PrepareAppliedCountAsync(db);
        InventoryCountDetails completed =
            (await new CompleteInventoryCount.Handler(
                db.DbContext,
                NullLogger<CompleteInventoryCount.Handler>.Instance)
            .HandleAsync(
                new CompleteInventoryCount.Command(
                    prepared.Count.Id,
                    prepared.Count.CountVersion,
                    InventoryCountTestData.ActorId),
                TestContext.Current.CancellationToken)).Value;

        ServiceResult<InventoryCountDetails> completeAgain =
            await new CompleteInventoryCount.Handler(
                db.DbContext,
                NullLogger<CompleteInventoryCount.Handler>.Instance)
            .HandleAsync(
                new CompleteInventoryCount.Command(
                    completed.Id,
                    completed.CountVersion,
                    InventoryCountTestData.ActorId),
                TestContext.Current.CancellationToken);
        ServiceResult<InventoryCountDetails> cancelCompleted =
            await new CancelInventoryCount.Handler(
                db.DbContext,
                NullLogger<CancelInventoryCount.Handler>.Instance)
            .HandleAsync(
                new CancelInventoryCount.Command(
                    completed.Id,
                    completed.CountVersion,
                    InventoryCountTestData.ActorId),
                TestContext.Current.CancellationToken);

        Assert.Equal(ServiceErrorType.Conflict, completeAgain.Error?.Type);
        Assert.Equal(ServiceErrorType.Conflict, cancelCompleted.Error?.Type);
    }

    private static async Task<PreparedLifecycleCount> PrepareAppliedCountAsync(
        TestWmsDbContext db)
    {
        SeededInventoryCountReferences references =
            await InventoryCountTestData.SeedReferencesAsync(db.DbContext);
        InventoryCountDetails created = (await new CreateInventoryCount.Handler(db.DbContext)
            .HandleAsync(
                new CreateInventoryCount.Command(
                    references.Warehouse.Id,
                    "Lifecycle count",
                    InventoryCountTestData.ActorId),
                TestContext.Current.CancellationToken)).Value;
        InventoryCountDetails added = (await new AddInventoryCountLine.Handler(db.DbContext)
            .HandleAsync(
                new AddInventoryCountLine.Command(
                    created.Id,
                    references.StockKeepingUnit.Id,
                    references.ExistingBalanceLocation.Id,
                    created.CountVersion,
                    InventoryCountTestData.ActorId),
                TestContext.Current.CancellationToken)).Value;
        InventoryCountLineDetails pending = Assert.Single(added.Lines);
        InventoryCountDetails recorded = (await new RecordInventoryCountLine.Handler(db.DbContext)
            .HandleAsync(
                new RecordInventoryCountLine.Command(
                    added.Id,
                    pending.Id,
                    CountedQuantity: 12,
                    Comment: null,
                    ExpectedLineVersion: pending.LineVersion,
                    ActorId: InventoryCountTestData.ActorId),
                TestContext.Current.CancellationToken)).Value;
        InventoryCountLineDetails counted = Assert.Single(recorded.Lines);
        InventoryCountDetails applied = (await new ApplyInventoryCountLine.Handler(
            db.DbContext,
            new RecordingDomainEventDispatcher(),
            NullLogger<ApplyInventoryCountLine.Handler>.Instance)
            .HandleAsync(
                new ApplyInventoryCountLine.Command(
                    recorded.Id,
                    counted.Id,
                    counted.LineVersion,
                    InventoryCountTestData.ActorId),
                TestContext.Current.CancellationToken)).Value;
        InventoryBalance balance = await db.DbContext.InventoryBalances.SingleAsync(
            x => x.Id == references.ExistingBalance!.Id,
            TestContext.Current.CancellationToken);
        InventoryTransaction transaction = await db.DbContext.InventoryTransactions.SingleAsync(
            TestContext.Current.CancellationToken);

        return new PreparedLifecycleCount(applied, balance, transaction);
    }

    private sealed record PreparedLifecycleCount(
        InventoryCountDetails Count,
        InventoryBalance Balance,
        InventoryTransaction Transaction);
}
