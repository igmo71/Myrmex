using Microsoft.EntityFrameworkCore;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryTransactions;
using Myrmex.Tests.Wms.DemoData.Testing;

namespace Myrmex.Tests.Wms.DemoData.Features;

public sealed class WmsDemoDataOperationalStateTests
{
    [Fact]
    public async Task SeedAsync_CreatesCoherentInventoryTransfersAndCounts()
    {
        await using DemoDataServiceFixture fixture = await DemoDataServiceFixture.CreateAsync();

        var result = await fixture.SeedAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(4, await fixture.DbContext.InventoryTransfers.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(2, await fixture.DbContext.InventoryCounts.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(5, await fixture.DbContext.InventoryCountLines.CountAsync(TestContext.Current.CancellationToken));
        Assert.True(await fixture.DbContext.InventoryTransactions
            .AnyAsync(x => x.TransactionType == InventoryTransactionType.Adjustment,
                TestContext.Current.CancellationToken));
        Assert.True(await fixture.DbContext.InventoryLedgerEntries
            .AllAsync(x => x.BalanceAfter == x.BalanceBefore + x.QuantityDelta,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SeedAsync_WhenStageFails_RollsBackWholeRequest()
    {
        await using DemoDataServiceFixture fixture = await DemoDataServiceFixture.CreateAsync(
            new ThrowAfterStageHook("seed", "openingInventory"));

        var result = await fixture.SeedAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(0, await fixture.DbContext.Warehouses.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, await fixture.DbContext.InventoryBalances.CountAsync(TestContext.Current.CancellationToken));
    }
}
