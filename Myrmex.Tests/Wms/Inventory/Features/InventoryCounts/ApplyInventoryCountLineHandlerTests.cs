using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryCounts;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryTransactions;
using Myrmex.Modules.Wms.Inventory.Features.InventoryCounts;
using Myrmex.Shared.Wms.Inventory;
using Myrmex.Tests.Wms.Inventory.Testing;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Inventory.Features.InventoryCounts;

public sealed class ApplyInventoryCountLineHandlerTests
{
    [Fact]
    public async Task Apply_WhenZeroVariance_MarksAppliedWithoutMovement()
    {
        await using TestWmsDbContext db = await TestWmsDbContext.CreateAsync();
        PreparedCount prepared = await PrepareAsync(db, countedQuantity: 10);
        byte[] balanceVersion = [.. prepared.References.ExistingBalance!.RowVersion];

        ServiceResult<InventoryCountDetails> result = await CreateApplyHandler(db).HandleAsync(
            new ApplyInventoryCountLine.Command(
                prepared.Count.Id,
                prepared.Line.Id,
                prepared.Line.LineVersion,
                InventoryCountTestData.ActorId),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        InventoryCountLineDetails line = Assert.Single(result.Value.Lines);
        Assert.Equal(InventoryCountLineStatusDetails.Applied, line.Status);
        Assert.Null(line.AppliedInventoryTransactionId);
        Assert.Equal(InventoryCountTestData.ActorId, line.AppliedByActorId);
        Assert.True(balanceVersion.SequenceEqual(prepared.References.ExistingBalance.RowVersion));
        Assert.Equal(0, await db.DbContext.InventoryTransactions.CountAsync(
            TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(14, 4)]
    [InlineData(6, -4)]
    public async Task Apply_WhenExistingBalanceVariance_CreatesExactAdjustment(
        decimal countedQuantity,
        decimal expectedDelta)
    {
        await using TestWmsDbContext db = await TestWmsDbContext.CreateAsync();
        PreparedCount prepared = await PrepareAsync(db, countedQuantity);

        ServiceResult<InventoryCountDetails> result = await CreateApplyHandler(db).HandleAsync(
            new ApplyInventoryCountLine.Command(
                prepared.Count.Id,
                prepared.Line.Id,
                prepared.Line.LineVersion,
                InventoryCountTestData.ActorId),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(countedQuantity, prepared.References.ExistingBalance!.Quantity);
        InventoryCountLineDetails line = Assert.Single(result.Value.Lines);
        Assert.NotNull(line.AppliedInventoryTransactionId);
        InventoryTransaction transaction = await db.DbContext.InventoryTransactions
            .Include(x => x.Entries)
            .SingleAsync(TestContext.Current.CancellationToken);
        InventoryLedgerEntry entry = Assert.Single(transaction.Entries);
        Assert.Equal(InventoryTransactionType.Adjustment, transaction.TransactionType);
        Assert.Contains(prepared.Count.Id.ToString(), transaction.Reason);
        Assert.Equal(expectedDelta, entry.QuantityDelta);
        Assert.Equal(10, entry.BalanceBefore);
        Assert.Equal(countedQuantity, entry.BalanceAfter);
        Assert.Equal(transaction.Id, line.AppliedInventoryTransactionId);
    }

    [Fact]
    public async Task Apply_WhenExpectedMissingAndPositive_CreatesBalanceAndAdjustment()
    {
        await using TestWmsDbContext db = await TestWmsDbContext.CreateAsync();
        PreparedCount prepared = await PrepareAsync(db, 7, useMissingLocation: true);

        ServiceResult<InventoryCountDetails> result = await CreateApplyHandler(db).HandleAsync(
            new ApplyInventoryCountLine.Command(
                prepared.Count.Id,
                prepared.Line.Id,
                prepared.Line.LineVersion,
                InventoryCountTestData.ActorId),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        InventoryBalance balance = await db.DbContext.InventoryBalances.SingleAsync(
            x => x.StorageLocationId == prepared.References.MissingBalanceLocation.Id,
            TestContext.Current.CancellationToken);
        Assert.Equal(7, balance.Quantity);
        InventoryLedgerEntry entry = await db.DbContext.InventoryLedgerEntries.SingleAsync(
            TestContext.Current.CancellationToken);
        Assert.Equal(7, entry.QuantityDelta);
        Assert.Equal(0, entry.BalanceBefore);
        Assert.Equal(7, entry.BalanceAfter);
    }

    [Fact]
    public async Task Apply_WhenSnapshotChanged_PersistsConflictWithoutMovement()
    {
        await using TestWmsDbContext db = await TestWmsDbContext.CreateAsync();
        PreparedCount prepared = await PrepareAsync(db, 12);
        prepared.References.ExistingBalance!.UpdateQuantity(11);
        await db.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        ServiceResult<InventoryCountDetails> result = await CreateApplyHandler(db).HandleAsync(
            new ApplyInventoryCountLine.Command(
                prepared.Count.Id,
                prepared.Line.Id,
                prepared.Line.LineVersion,
                InventoryCountTestData.ActorId),
            TestContext.Current.CancellationToken);

        Assert.Equal(ServiceErrorType.Conflict, result.Error.Type);
        db.DbContext.ChangeTracker.Clear();
        InventoryCountLine line = await db.DbContext.InventoryCountLines.SingleAsync(
            x => x.Id == prepared.Line.Id,
            TestContext.Current.CancellationToken);
        Assert.Equal(InventoryCountLineStatus.Conflict, line.Status);
        Assert.Equal(0, await db.DbContext.InventoryTransactions.CountAsync(
            TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(SnapshotPresenceChange.Appeared)]
    [InlineData(SnapshotPresenceChange.Disappeared)]
    public async Task Apply_WhenBalancePresenceChanged_PersistsConflict(
        SnapshotPresenceChange change)
    {
        await using TestWmsDbContext db = await TestWmsDbContext.CreateAsync();
        PreparedCount prepared = await PrepareAsync(
            db,
            countedQuantity: 7,
            useMissingLocation: change == SnapshotPresenceChange.Appeared);

        if (change == SnapshotPresenceChange.Appeared)
        {
            var createResult = InventoryBalance.Create(
                prepared.References.StockKeepingUnit.Id,
                prepared.References.MissingBalanceLocation.Id,
                1,
                out InventoryBalance? appeared);
            Assert.True(createResult.IsValid);
            appeared!.ClearDomainEvents();
            db.DbContext.InventoryBalances.Add(appeared);
        }
        else
        {
            db.DbContext.InventoryBalances.Remove(
                prepared.References.ExistingBalance!);
        }
        await db.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        ServiceResult<InventoryCountDetails> result = await CreateApplyHandler(db).HandleAsync(
            new ApplyInventoryCountLine.Command(
                prepared.Count.Id,
                prepared.Line.Id,
                prepared.Line.LineVersion,
                InventoryCountTestData.ActorId),
            TestContext.Current.CancellationToken);

        Assert.Equal(ServiceErrorType.Conflict, result.Error?.Type);
        db.DbContext.ChangeTracker.Clear();
        Assert.Equal(
            InventoryCountLineStatus.Conflict,
            (await db.DbContext.InventoryCountLines.SingleAsync(
                x => x.Id == prepared.Line.Id,
                TestContext.Current.CancellationToken)).Status);
        Assert.Equal(0, await db.DbContext.InventoryTransactions.CountAsync(
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Apply_WhenRepeated_ReturnsConflictWithoutDuplicateTransaction()
    {
        await using TestWmsDbContext db = await TestWmsDbContext.CreateAsync();
        PreparedCount prepared = await PrepareAsync(db, 12);
        ApplyInventoryCountLine.Handler handler = CreateApplyHandler(db);

        ServiceResult<InventoryCountDetails> first = await handler.HandleAsync(
            new ApplyInventoryCountLine.Command(
                prepared.Count.Id,
                prepared.Line.Id,
                prepared.Line.LineVersion,
                InventoryCountTestData.ActorId),
            TestContext.Current.CancellationToken);
        ServiceResult<InventoryCountDetails> second = await handler.HandleAsync(
            new ApplyInventoryCountLine.Command(
                prepared.Count.Id,
                prepared.Line.Id,
                prepared.Line.LineVersion,
                InventoryCountTestData.ActorId),
            TestContext.Current.CancellationToken);

        Assert.True(first.IsSuccess);
        Assert.Equal(ServiceErrorType.Conflict, second.Error?.Type);
        Assert.Equal(1, await db.DbContext.InventoryTransactions.CountAsync(
            TestContext.Current.CancellationToken));
        Assert.Equal(1, await db.DbContext.InventoryLedgerEntries.CountAsync(
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Supersede_WhenConflict_CreatesFreshPendingReplacementAndRejectsDuplicate()
    {
        await using TestWmsDbContext db = await TestWmsDbContext.CreateAsync();
        PreparedCount prepared = await PrepareAsync(db, 12);
        prepared.References.ExistingBalance!.UpdateQuantity(11);
        await db.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        await CreateApplyHandler(db).HandleAsync(
            new ApplyInventoryCountLine.Command(
                prepared.Count.Id,
                prepared.Line.Id,
                prepared.Line.LineVersion,
                InventoryCountTestData.ActorId),
            TestContext.Current.CancellationToken);
        db.DbContext.ChangeTracker.Clear();
        InventoryCountDetails conflict = (await new GetInventoryCountById.Handler(db.DbContext)
            .HandleAsync(
                new GetInventoryCountById.Query(prepared.Count.Id),
                TestContext.Current.CancellationToken)).Value;
        InventoryCountLineDetails conflictLine = Assert.Single(conflict.Lines);
        var handler = new SupersedeInventoryCountLine.Handler(
            db.DbContext,
            NullLogger<SupersedeInventoryCountLine.Handler>.Instance);

        ServiceResult<InventoryCountDetails> result = await handler.HandleAsync(
            new SupersedeInventoryCountLine.Command(
                prepared.Count.Id,
                conflictLine.Id,
                conflictLine.LineVersion,
                InventoryCountTestData.ActorId),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        InventoryCountLineDetails superseded = Assert.Single(
            result.Value.Lines,
            x => x.Status == InventoryCountLineStatusDetails.Superseded);
        InventoryCountLineDetails replacement = Assert.Single(
            result.Value.Lines,
            x => x.Status == InventoryCountLineStatusDetails.Pending);
        Assert.False(superseded.IsCurrent);
        Assert.True(replacement.IsCurrent);
        Assert.Equal(11, replacement.SystemQuantity);
        Assert.Equal(superseded.Id, replacement.SupersedesInventoryCountLineId);
        Assert.Equal(replacement.Id, superseded.ReplacementInventoryCountLineId);

        ServiceResult<InventoryCountDetails> duplicate = await handler.HandleAsync(
            new SupersedeInventoryCountLine.Command(
                prepared.Count.Id,
                conflictLine.Id,
                conflictLine.LineVersion,
                InventoryCountTestData.ActorId),
            TestContext.Current.CancellationToken);
        Assert.Equal(ServiceErrorType.Conflict, duplicate.Error.Type);
    }

    private static ApplyInventoryCountLine.Handler CreateApplyHandler(TestWmsDbContext db) =>
        new(
            db.DbContext,
            new RecordingDomainEventDispatcher(),
            NullLogger<ApplyInventoryCountLine.Handler>.Instance);

    private static async Task<PreparedCount> PrepareAsync(
        TestWmsDbContext db,
        decimal countedQuantity,
        bool useMissingLocation = false)
    {
        SeededInventoryCountReferences references =
            await InventoryCountTestData.SeedReferencesAsync(db.DbContext);
        InventoryCountDetails created = (await new CreateInventoryCount.Handler(db.DbContext)
            .HandleAsync(
                new CreateInventoryCount.Command(
                    references.Warehouse.Id,
                    "Cycle count",
                    InventoryCountTestData.ActorId),
                TestContext.Current.CancellationToken)).Value;
        InventoryCountDetails added = (await new AddInventoryCountLine.Handler(db.DbContext)
            .HandleAsync(
                new AddInventoryCountLine.Command(
                    created.Id,
                    references.StockKeepingUnit.Id,
                    useMissingLocation
                        ? references.MissingBalanceLocation.Id
                        : references.ExistingBalanceLocation.Id,
                    created.CountVersion,
                    InventoryCountTestData.ActorId),
                TestContext.Current.CancellationToken)).Value;
        InventoryCountLineDetails pending = Assert.Single(added.Lines);
        InventoryCountDetails recorded = (await new RecordInventoryCountLine.Handler(db.DbContext)
            .HandleAsync(
                new RecordInventoryCountLine.Command(
                    added.Id,
                    pending.Id,
                    countedQuantity,
                    "Count evidence",
                    pending.LineVersion,
                    InventoryCountTestData.ActorId),
                TestContext.Current.CancellationToken)).Value;

        return new PreparedCount(recorded, Assert.Single(recorded.Lines), references);
    }

    private sealed record PreparedCount(
        InventoryCountDetails Count,
        InventoryCountLineDetails Line,
        SeededInventoryCountReferences References);

    public enum SnapshotPresenceChange
    {
        Appeared,
        Disappeared
    }
}
