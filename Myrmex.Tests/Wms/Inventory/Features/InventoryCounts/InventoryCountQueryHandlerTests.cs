using Myrmex.Core.Domain;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryCounts;
using Myrmex.Modules.Wms.Inventory.Features.InventoryCounts;
using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Inventory;
using Myrmex.Tests.Wms.Inventory.Testing;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Inventory.Features.InventoryCounts;

public sealed class InventoryCountQueryHandlerTests
{
    [Fact]
    public async Task List_AppliesFiltersBeforePagingAndProjectsCurrentProgress()
    {
        await using TestWmsDbContext db = await TestWmsDbContext.CreateAsync();

        SeededInventoryCountReferences references =
            await InventoryCountTestData.SeedReferencesAsync(db.DbContext);

        InventoryCount older = await CreateCountAsync(
            db,
            references.Warehouse.Id,
            "Older",
            DateTimeOffset.Parse("2026-06-20T08:00:00Z"));

        InventoryCount newer = await CreateCountAsync(
            db,
            references.Warehouse.Id,
            "Newer",
            DateTimeOffset.Parse("2026-06-21T08:00:00Z"));

        await CreateCountAsync(
            db,
            references.SecondWarehouse.Id,
            "Other warehouse",
            DateTimeOffset.Parse("2026-06-22T08:00:00Z"));

        var addLineResult = newer.AddLine(
            references.StockKeepingUnit.Id,
            references.ExistingBalanceLocation.Id,
            10,
            references.ExistingBalance!.RowVersion,
            out InventoryCountLine? applied);

        Assert.True(addLineResult.IsValid);

        db.DbContext.Add(applied!);

        Assert.True(newer.RecordLineCount(
            applied!.Id,
            10,
            "verified",
            "counter-1",
            DateTimeOffset.Parse("2026-06-21T09:00:00Z")).IsValid);

        Assert.True(newer.ApplyLine(
            applied.Id,
            null,
            "applier-1",
            DateTimeOffset.Parse("2026-06-21T10:00:00Z")).IsValid);

        addLineResult = newer.AddLine(
            references.StockKeepingUnit.Id,
            references.MissingBalanceLocation.Id,
            0,
            null,
            out InventoryCountLine? unresolved);

        db.DbContext.Add(unresolved!);

        Assert.True(addLineResult.IsValid);

        await db.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ListInventoryCounts.Handler(db.DbContext);
        ServiceResult<ListResult<InventoryCountListItem>> result =
            await handler.HandleAsync(
                new ListInventoryCounts.Query
                {
                    Skip = 0,
                    Take = 1,
                    SortBy = InventoryCountSortBy.CreatedAtUtc,
                    SortDescending = true,
                    WarehouseId = references.Warehouse.Id,
                    StatusText = InventoryCountStatusDetails.InProgress,
                    Status = InventoryCountStatus.InProgress,
                    CreatedFromUtc = DateTimeOffset.Parse("2026-06-21T00:00:00Z"),
                    CreatedToUtc = DateTimeOffset.Parse("2026-06-21T23:59:59Z")
                },
                TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalCount);
        InventoryCountListItem item = Assert.Single(result.Value.Items);
        Assert.Equal(newer.Id, item.Id);
        Assert.Equal(2, item.LineCount);
        Assert.Equal(1, item.AppliedLineCount);
        Assert.Equal(1, item.UnresolvedLineCount);
        Assert.Equal(0, item.ConflictLineCount);
        Assert.False(string.IsNullOrWhiteSpace(item.CountVersion));
        Assert.NotEqual(older.Id, item.Id);
    }

    [Fact]
    public async Task List_WhenSortByCreatedAtUtcDescending_OrdersByCreatedAtUtcDescending()
    {
        await using TestWmsDbContext db = await TestWmsDbContext.CreateAsync();

        SeededInventoryCountReferences references =
            await InventoryCountTestData.SeedReferencesAsync(db.DbContext);

        InventoryCount first = await CreateCountAsync(
            db,
            references.Warehouse.Id,
            "First",
            DateTimeOffset.Parse("2026-06-20T08:00:00Z"));

        InventoryCount second = await CreateCountAsync(
            db,
            references.Warehouse.Id,
            "Second",
            DateTimeOffset.Parse("2026-06-20T09:00:00Z"));

        var handler = new ListInventoryCounts.Handler(db.DbContext);

        ServiceResult<ListResult<InventoryCountListItem>> result =
            await handler.HandleAsync(
                new ListInventoryCounts.Query
                {
                    SortBy = InventoryCountSortBy.CreatedAtUtc,
                    SortDescending = true
                },
                TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);

        Assert.Equal(
            [second.Id, first.Id],
            result.Value.Items.Select(x => x.Id));
    }

    [Fact]
    public async Task List_WhenCreatedAtUtcEqual_ReturnsStableOrder()
    {
        await using TestWmsDbContext db = await TestWmsDbContext.CreateAsync();

        SeededInventoryCountReferences references =
            await InventoryCountTestData.SeedReferencesAsync(db.DbContext);

        DateTimeOffset sameCreatedAt = DateTimeOffset.Parse("2026-06-20T08:00:00Z");

        await CreateCountAsync(
            db,
            references.Warehouse.Id,
            "First",
            sameCreatedAt);

        await CreateCountAsync(
            db,
            references.Warehouse.Id,
            "Second",
            sameCreatedAt);

        var handler = new ListInventoryCounts.Handler(db.DbContext);

        ServiceResult<ListResult<InventoryCountListItem>> firstResult =
            await handler.HandleAsync(
                new ListInventoryCounts.Query
                {
                    SortBy = InventoryCountSortBy.CreatedAtUtc,
                    SortDescending = true
                },
                TestContext.Current.CancellationToken);

        ServiceResult<ListResult<InventoryCountListItem>> secondResult =
            await handler.HandleAsync(
                new ListInventoryCounts.Query
                {
                    SortBy = InventoryCountSortBy.CreatedAtUtc,
                    SortDescending = true
                },
                TestContext.Current.CancellationToken);

        Assert.True(firstResult.IsSuccess);
        Assert.True(secondResult.IsSuccess);

        Assert.Equal(
            firstResult.Value.Items.Select(x => x.Id),
            secondResult.Value.Items.Select(x => x.Id));
    }

    [Fact]
    public async Task List_WhenFiltersInvalid_ReturnsInvalidResult()
    {
        await using TestWmsDbContext db = await TestWmsDbContext.CreateAsync();

        var handler = new ListInventoryCounts.Handler(db.DbContext);

        ServiceResult<ListResult<InventoryCountListItem>> invalidStatus =
            await handler.HandleAsync(
                new ListInventoryCounts.Query
                {
                    StatusText = "Unknown"
                },
                TestContext.Current.CancellationToken);

        ServiceResult<ListResult<InventoryCountListItem>> invalidDates =
            await handler.HandleAsync(
                new ListInventoryCounts.Query
                {
                    CreatedFromUtc = DateTimeOffset.Parse("2026-06-22T00:00:00Z"),
                    CreatedToUtc = DateTimeOffset.Parse("2026-06-21T00:00:00Z")
                },
                TestContext.Current.CancellationToken);

        Assert.False(invalidStatus.IsSuccess);
        Assert.Equal(ServiceErrorType.Invalid, invalidStatus.Error.Type);

        Assert.False(invalidDates.IsSuccess);
        Assert.Equal(ServiceErrorType.Invalid, invalidDates.Error.Type);
    }

    [Fact]
    public async Task Details_ReturnsSupersededHistoryAndInactiveReferenceLabels()
    {
        await using TestWmsDbContext db = await TestWmsDbContext.CreateAsync();
        SeededInventoryCountReferences references =
            await InventoryCountTestData.SeedReferencesAsync(db.DbContext);
        InventoryCount count = await CreateCountAsync(
            db,
            references.Warehouse.Id,
            "History",
            DateTimeOffset.Parse("2026-06-20T08:00:00Z"));

        var addLineResult = count.AddLine(
            references.StockKeepingUnit.Id,
            references.ExistingBalanceLocation.Id,
            10,
            references.ExistingBalance!.RowVersion,
            out InventoryCountLine? original);
        Assert.True(addLineResult.IsValid);

        db.DbContext.Add(original!);

        Assert.True(count.RecordLineCount(
            original!.Id,
            11,
            "history evidence",
            "counter-1",
            DateTimeOffset.Parse("2026-06-20T09:00:00Z")).IsValid);
        Assert.True(count.MarkLineConflict(original.Id).IsValid);

        var supersedeLineResult = count.SupersedeLine(
            original.Id,
            12,
            references.ExistingBalance.RowVersion,
            out InventoryCountLine? replacement);

        Assert.True(supersedeLineResult.IsValid);

        db.DbContext.Add(replacement!);

        references.StockKeepingUnit.Deactivate();
        references.ExistingBalanceLocation.Deactivate();
        await db.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.DbContext.ChangeTracker.Clear();

        ServiceResult<InventoryCountDetails> result =
            await new GetInventoryCountById.Handler(db.DbContext).HandleAsync(
                new GetInventoryCountById.Query(count.Id),
                TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Lines.Count);
        InventoryCountLineDetails historical = result.Value.Lines.Single(x => x.Id == original.Id);
        Assert.Equal(InventoryCountLineStatusDetails.Superseded, historical.Status);
        Assert.False(historical.IsCurrent);
        Assert.Equal(replacement!.Id, historical.ReplacementInventoryCountLineId);
        Assert.Equal("history evidence", historical.Comment);
        Assert.Equal(references.StockKeepingUnit.Code, historical.Sku.Code);
        Assert.Equal(references.ExistingBalanceLocation.Code, historical.StorageLocation.Code);
        Assert.False(string.IsNullOrWhiteSpace(historical.LineVersion));
    }

    [Fact]
    public async Task List_WhenCancellationRequested_PropagatesCancellation()
    {
        await using TestWmsDbContext db = await TestWmsDbContext.CreateAsync();
        using CancellationTokenSource cancellationTokenSource = new();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new ListInventoryCounts.Handler(db.DbContext).HandleAsync(
                new ListInventoryCounts.Query(),
                cancellationTokenSource.Token));
    }

    private static async Task<InventoryCount> CreateCountAsync(
        TestWmsDbContext db,
        Guid warehouseId,
        string reason,
        DateTimeOffset createdAtUtc)
    {
        InventoryCount count = await InventoryCountTestData.CreateCountAsync(
            db.DbContext,
            warehouseId,
            reason);
        typeof(EntityBase)
            .GetProperty(nameof(EntityBase.CreatedAtUtc))!
            .SetValue(count, createdAtUtc);
        await db.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return count;
    }
}
