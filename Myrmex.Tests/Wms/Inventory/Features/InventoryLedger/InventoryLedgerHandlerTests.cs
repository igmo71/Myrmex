using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryTransactions;
using Myrmex.Modules.Wms.Inventory.Features.InventoryLedger;
using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Inventory;
using Myrmex.Tests.Wms.Inventory.Testing;
using Myrmex.Tests.Wms.Topology.Testing;
using System.Data.SqlTypes;

namespace Myrmex.Tests.Wms.Inventory.Features.InventoryLedger;

public sealed class InventoryLedgerHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenNoFiltersProvided_ReturnsNewestFirstWithCountBeforePaging()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededInventoryLedger seeded = await InventoryLedgerTestData.SeedLedgerAsync(testDbContext.DbContext);
        ListInventoryLedgerEntries.Handler handler = new(testDbContext.DbContext);

        ServiceResult<ListResult<InventoryLedgerEntryDetails>> result = await handler.HandleAsync(
            new ListInventoryLedgerEntries.Query
            {
                Skip = 1,
                Take = 2
            },
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(seeded.Entries.Count, result.Value.TotalCount);
        Assert.Equal(1, result.Value.Skip);
        Assert.Equal(2, result.Value.Take);
        AssertIdsEqual(
            ExpectedDefaultOrder(seeded).Skip(1).Take(2),
            result.Value.Items.Select(x => x.EntryId),
            "default paged order");
    }

    [Fact]
    public async Task HandleAsync_WhenNoRowsMatch_ReturnsEmptySuccessfulPage()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        await InventoryLedgerTestData.SeedLedgerAsync(testDbContext.DbContext);
        ListInventoryLedgerEntries.Handler handler = new(testDbContext.DbContext);

        ServiceResult<ListResult<InventoryLedgerEntryDetails>> result = await handler.HandleAsync(
            new ListInventoryLedgerEntries.Query
            {
                StockKeepingUnitId = Guid.NewGuid()
            },
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalCount);
    }

    [Fact]
    public async Task HandleAsync_WhenProjectionReturned_IncludesRequiredNestedFields()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededInventoryLedger seeded = await InventoryLedgerTestData.SeedLedgerAsync(testDbContext.DbContext);
        ListInventoryLedgerEntries.Handler handler = new(testDbContext.DbContext);
        InventoryLedgerEntry expectedEntry = seeded.Oldest.Entries.Single();

        ServiceResult<ListResult<InventoryLedgerEntryDetails>> result = await handler.HandleAsync(
            new ListInventoryLedgerEntries.Query
            {
                StorageLocationId = seeded.LocationA.Id
            },
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        InventoryLedgerEntryDetails item = Assert.Single(result.Value.Items);
        Assert.Equal(expectedEntry.Id, item.EntryId);
        Assert.Equal(seeded.Oldest.Id, item.TransactionId);
        Assert.Equal(nameof(InventoryTransactionType.Adjustment), item.TransactionType);
        Assert.Equal(seeded.Oldest.Reason, item.Reason);
        Assert.Equal(seeded.Oldest.OccurredAtUtc, item.OccurredAtUtc);
        Assert.Equal(expectedEntry.BalanceBefore, item.BalanceBefore);
        Assert.Equal(expectedEntry.QuantityDelta, item.QuantityDelta);
        Assert.Equal(expectedEntry.BalanceAfter, item.BalanceAfter);
        Assert.Equal(seeded.SkuA.Id, item.Sku.Id);
        Assert.Equal(seeded.SkuA.Code, item.Sku.Code);
        Assert.Equal(seeded.SkuA.Name, item.Sku.Name);
        Assert.Equal(seeded.Each.Id, item.Sku.BaseUom.Id);
        Assert.Equal(seeded.Each.Code, item.Sku.BaseUom.Code);
        Assert.Equal(seeded.Each.Symbol, item.Sku.BaseUom.Symbol);
        Assert.Equal(seeded.LocationA.Id, item.StorageLocation.Id);
        Assert.Equal(seeded.LocationA.Code, item.StorageLocation.Code);
        Assert.Equal(seeded.WarehouseA.Id, item.StorageLocation.Warehouse.Id);
        Assert.Equal(seeded.WarehouseA.Code, item.StorageLocation.Warehouse.Code);
    }

    [Fact]
    public async Task HandleAsync_WhenNoCurrentBalanceExists_StillReturnsLedgerHistory()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        await InventoryLedgerTestData.SeedLedgerAsync(testDbContext.DbContext);
        ListInventoryLedgerEntries.Handler handler = new(testDbContext.DbContext);

        Assert.False(testDbContext.DbContext.InventoryBalances.Any());

        ServiceResult<ListResult<InventoryLedgerEntryDetails>> result = await handler.HandleAsync(
            new ListInventoryLedgerEntries.Query(),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value.Items);
    }

    [Theory]
    [MemberData(nameof(FilterCases))]
    public async Task HandleAsync_WhenFilterProvided_AppliesSupportedFilter(FilterScenario scenario)
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededInventoryLedger seeded = await InventoryLedgerTestData.SeedLedgerAsync(testDbContext.DbContext);
        ListInventoryLedgerEntries.Handler handler = new(testDbContext.DbContext);
        (ListInventoryLedgerEntries.Query Query, Guid[] ExpectedEntryIds) = CreateFilterCase(seeded, scenario);

        ServiceResult<ListResult<InventoryLedgerEntryDetails>> result = await handler.HandleAsync(
            Query,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(ExpectedEntryIds.Length, result.Value.TotalCount);
        AssertIdsEqual(ExpectedEntryIds, result.Value.Items.Select(x => x.EntryId), scenario.ToString());
    }

    [Fact]
    public async Task HandleAsync_WhenInactiveReferencesExist_ReturnsVisibleHistory()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededInventoryLedger seeded = await InventoryLedgerTestData.SeedLedgerAsync(testDbContext.DbContext);
        ListInventoryLedgerEntries.Handler handler = new(testDbContext.DbContext);

        ServiceResult<ListResult<InventoryLedgerEntryDetails>> result = await handler.HandleAsync(
            new ListInventoryLedgerEntries.Query
            {
                StockKeepingUnitId = seeded.InactiveSku.Id
            },
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        InventoryLedgerEntryDetails item = Assert.Single(result.Value.Items);
        Assert.Equal(seeded.InactiveSku.Id, item.Sku.Id);
        Assert.Equal(seeded.InactiveSku.Code, item.Sku.Code);
        Assert.Equal(seeded.InactiveUom.Id, item.Sku.BaseUom.Id);
        Assert.Equal(seeded.InactiveWarehouse.Id, item.StorageLocation.Warehouse.Id);
        Assert.Equal(seeded.InactiveWarehouseLocation.Id, item.StorageLocation.Id);
    }

    [Theory]
    [MemberData(nameof(SortCases))]
    public async Task HandleAsync_WhenSortBySupportedKey_OrdersByRequestedKeyThenTransactionAndEntry(
        string sortBy,
        bool sortDescending)
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededInventoryLedger seeded = await InventoryLedgerTestData.SeedLedgerAsync(testDbContext.DbContext);
        ListInventoryLedgerEntries.Handler handler = new(testDbContext.DbContext);

        ServiceResult<ListResult<InventoryLedgerEntryDetails>> result = await handler.HandleAsync(
            new ListInventoryLedgerEntries.Query
            {
                SortBy = sortBy,
                SortDescending = sortDescending
            },
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        AssertIdsEqual(
            ExpectedRequestedSortOrder(seeded, sortBy, sortDescending),
            result.Value.Items.Select(x => x.EntryId),
            $"{sortBy} descending={sortDescending}");
    }

    [Fact]
    public async Task HandleAsync_WhenOccurrenceBoundariesAreEqual_ReturnsValidEmptyInterval()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        await InventoryLedgerTestData.SeedLedgerAsync(testDbContext.DbContext);
        ListInventoryLedgerEntries.Handler handler = new(testDbContext.DbContext);
        DateTimeOffset boundary = DateTimeOffset.Parse("2026-06-18T09:00:00+00:00");

        ServiceResult<ListResult<InventoryLedgerEntryDetails>> result = await handler.HandleAsync(
            new ListInventoryLedgerEntries.Query
            {
                OccurredFromUtc = boundary,
                OccurredToUtc = boundary
            },
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalCount);
    }

    [Theory]
    [MemberData(nameof(InvalidCases))]
    internal async Task HandleAsync_WhenRequestInvalid_ReturnsValidationError(ListInventoryLedgerEntries.Query query)
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        ListInventoryLedgerEntries.Handler handler = new(testDbContext.DbContext);

        ServiceResult<ListResult<InventoryLedgerEntryDetails>> result = await handler.HandleAsync(
            query,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorType.Invalid, result.Error.Type);
    }

    public static IEnumerable<object[]> FilterCases()
    {
        yield return [FilterScenario.StockKeepingUnit];
        yield return [FilterScenario.Warehouse];
        yield return [FilterScenario.StorageLocation];
        yield return [FilterScenario.TransactionType];
        yield return [FilterScenario.OccurrenceRange];
        yield return [FilterScenario.Combined];
        yield return [FilterScenario.NoMatch];
    }

    public static IEnumerable<object[]> SortCases()
    {
        string[] sortKeys =
        [
            InventoryLedgerSortBy.OccurredAtUtc,
            InventoryLedgerSortBy.TransactionType,
            InventoryLedgerSortBy.SkuCode,
            InventoryLedgerSortBy.SkuName,
            InventoryLedgerSortBy.WarehouseCode,
            InventoryLedgerSortBy.WarehouseName,
            InventoryLedgerSortBy.StorageLocationCode,
            InventoryLedgerSortBy.BalanceBefore,
            InventoryLedgerSortBy.QuantityDelta,
            InventoryLedgerSortBy.BalanceAfter,
            InventoryLedgerSortBy.Reason
        ];

        foreach (string sortKey in sortKeys)
        {
            yield return [sortKey, false];
            yield return [sortKey, true];
        }
    }

    public static IEnumerable<object[]> InvalidCases()
    {
        yield return
        [
            new ListInventoryLedgerEntries.Query
            {
                TransactionType = "Transfer"
            }
        ];
        yield return
        [
            new ListInventoryLedgerEntries.Query
            {
                OccurredFromUtc = DateTimeOffset.Parse("2026-06-18T10:00:00+00:00"),
                OccurredToUtc = DateTimeOffset.Parse("2026-06-18T09:00:00+00:00")
            }
        ];
    }

    private static (ListInventoryLedgerEntries.Query Query, Guid[] ExpectedEntryIds) CreateFilterCase(
        SeededInventoryLedger seeded,
        FilterScenario scenario)
    {
        return scenario switch
        {
            FilterScenario.StockKeepingUnit => (
                new ListInventoryLedgerEntries.Query
                {
                    StockKeepingUnitId = seeded.SkuA.Id
                },
                EntriesFor(seeded.Oldest, seeded.SameTimeSecond)),

            FilterScenario.Warehouse => (
                new ListInventoryLedgerEntries.Query
                {
                    WarehouseId = seeded.WarehouseB.Id
                },
                EntriesFor(seeded.SameTimeFirst, seeded.SameTimeSecond)),

            FilterScenario.StorageLocation => (
                new ListInventoryLedgerEntries.Query
                {
                    StorageLocationId = seeded.LocationB.Id
                },
                EntriesFor(seeded.SameTimeFirst, seeded.SameTimeSecond)),

            FilterScenario.TransactionType => (
                new ListInventoryLedgerEntries.Query
                {
                    TransactionType = nameof(InventoryTransactionType.Adjustment)
                },
                ExpectedDefaultOrder(seeded).ToArray()),

            FilterScenario.OccurrenceRange => (
                new ListInventoryLedgerEntries.Query
                {
                    OccurredFromUtc = DateTimeOffset.Parse("2026-06-18T09:00:00+00:00"),
                    OccurredToUtc = DateTimeOffset.Parse("2026-06-18T10:00:00+00:00")
                },
                EntriesFor(seeded.SameTimeFirst, seeded.SameTimeSecond)),

            FilterScenario.Combined => (
                new ListInventoryLedgerEntries.Query
                {
                    StockKeepingUnitId = seeded.SkuA.Id,
                    WarehouseId = seeded.WarehouseB.Id,
                    StorageLocationId = seeded.LocationB.Id
                },
                EntriesFor(seeded.SameTimeSecond)),

            FilterScenario.NoMatch => (
                new ListInventoryLedgerEntries.Query
                {
                    WarehouseId = Guid.NewGuid()
                },
                []),

            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null)
        };
    }

    private static Guid[] ExpectedDefaultOrder(SeededInventoryLedger seeded)
    {
        return seeded.Transactions
            .SelectMany(transaction => transaction.Entries.Select(entry => new { Transaction = transaction, Entry = entry }))
            .OrderByDescending(x => x.Transaction.OccurredAtUtc)
            .ThenByDescending(x => new SqlGuid(x.Transaction.Id))
            .ThenByDescending(x => new SqlGuid(x.Entry.Id))
            .Select(x => x.Entry.Id)
            .ToArray();
    }

    private static Guid[] ExpectedRequestedSortOrder(
        SeededInventoryLedger seeded,
        string sortBy,
        bool sortDescending)
    {
        IEnumerable<ExpectedLedgerEntry> entries = ExpectedEntries(seeded);

        IOrderedEnumerable<ExpectedLedgerEntry> ordered = sortBy switch
        {
            InventoryLedgerSortBy.OccurredAtUtc => Sort(entries, x => x.Transaction.OccurredAtUtc, sortDescending),
            InventoryLedgerSortBy.TransactionType => Sort(entries, x => x.Transaction.TransactionType, sortDescending),
            InventoryLedgerSortBy.SkuCode => Sort(entries, x => x.Sku.Code, sortDescending),
            InventoryLedgerSortBy.SkuName => Sort(entries, x => x.Sku.Name, sortDescending),
            InventoryLedgerSortBy.WarehouseCode => Sort(entries, x => x.Warehouse.Code, sortDescending),
            InventoryLedgerSortBy.WarehouseName => Sort(entries, x => x.Warehouse.Name, sortDescending),
            InventoryLedgerSortBy.StorageLocationCode => Sort(entries, x => x.StorageLocation.Code, sortDescending),
            InventoryLedgerSortBy.BalanceBefore => Sort(entries, x => x.Entry.BalanceBefore, sortDescending),
            InventoryLedgerSortBy.QuantityDelta => Sort(entries, x => x.Entry.QuantityDelta, sortDescending),
            InventoryLedgerSortBy.BalanceAfter => Sort(entries, x => x.Entry.BalanceAfter, sortDescending),
            InventoryLedgerSortBy.Reason => Sort(entries, x => x.Transaction.Reason, sortDescending),
            _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, null)
        };

        return ordered
            .ThenBy(x => new SqlGuid(x.Transaction.Id))
            .ThenBy(x => new SqlGuid(x.Entry.Id))
            .Select(x => x.Entry.Id)
            .ToArray();
    }

    private static IReadOnlyList<ExpectedLedgerEntry> ExpectedEntries(SeededInventoryLedger seeded)
    {
        return
        [
            new(
                seeded.Oldest,
                seeded.Oldest.Entries.Single(),
                seeded.SkuA,
                seeded.WarehouseA,
                seeded.LocationA),
            new(
                seeded.SameTimeFirst,
                seeded.SameTimeFirst.Entries.Single(),
                seeded.SkuB,
                seeded.WarehouseB,
                seeded.LocationB),
            new(
                seeded.SameTimeSecond,
                seeded.SameTimeSecond.Entries.Single(),
                seeded.SkuA,
                seeded.WarehouseB,
                seeded.LocationB),
            new(
                seeded.InactiveReferences,
                seeded.InactiveReferences.Entries.Single(),
                seeded.InactiveSku,
                seeded.InactiveWarehouse,
                seeded.InactiveWarehouseLocation)
        ];
    }

    private static IOrderedEnumerable<T> Sort<T, TKey>(
        IEnumerable<T> values,
        Func<T, TKey> keySelector,
        bool sortDescending)
    {
        return sortDescending
            ? values.OrderByDescending(keySelector)
            : values.OrderBy(keySelector);
    }

    private static Guid[] EntriesFor(params InventoryTransaction[] transactions)
    {
        return transactions
            .SelectMany(x => x.Entries)
            .Select(x => x.Id)
            .ToArray();
    }

    private static void AssertIdsEqual(
        IEnumerable<Guid> expected,
        IEnumerable<Guid> actual,
        string scenario)
    {
        Guid[] expectedIds = expected.ToArray();
        Guid[] actualIds = actual.ToArray();

        Assert.Equivalent(
        expected.ToArray(),
        actual.ToArray(),
        strict: true);
    }

    public enum FilterScenario
    {
        StockKeepingUnit,
        Warehouse,
        StorageLocation,
        TransactionType,
        OccurrenceRange,
        Combined,
        NoMatch
    }

    private sealed record ExpectedLedgerEntry(
        InventoryTransaction Transaction,
        InventoryLedgerEntry Entry,
        Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits.StockKeepingUnit Sku,
        Myrmex.Modules.Wms.Topology.Domain.Warehouses.Warehouse Warehouse,
        Myrmex.Modules.Wms.Topology.Domain.StorageLocations.StorageLocation StorageLocation);
}
