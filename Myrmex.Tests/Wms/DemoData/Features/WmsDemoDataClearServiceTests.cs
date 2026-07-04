using Microsoft.EntityFrameworkCore;
using Myrmex.Tests.Wms.DemoData.Testing;

namespace Myrmex.Tests.Wms.DemoData.Features;

public sealed class WmsDemoDataClearServiceTests
{
    [Fact]
    public async Task ClearAsync_DeletesMutableDataAndPreservesSystemReferences()
    {
        await using DemoDataServiceFixture fixture = await DemoDataServiceFixture.CreateAsync();
        Assert.True((await fixture.SeedAsync()).IsSuccess);
        int typeCount = await fixture.DbContext.StorageLocationTypes.CountAsync(TestContext.Current.CancellationToken);
        int statusCount = await fixture.DbContext.StorageLocationStatuses.CountAsync(TestContext.Current.CancellationToken);

        var result = await fixture.ClearAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(0, await fixture.DbContext.StockKeepingUnits.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, await fixture.DbContext.Warehouses.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, await fixture.DbContext.InventoryTransactions.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(typeCount, await fixture.DbContext.StorageLocationTypes.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(statusCount, await fixture.DbContext.StorageLocationStatuses.CountAsync(TestContext.Current.CancellationToken));
        Assert.True(result.Value.Areas.Sum(x => x.Deleted) > 0);
    }

    [Fact]
    public async Task ClearAsync_WhenStageFails_RollsBackEveryDeletion()
    {
        await using DemoDataServiceFixture fixture = await DemoDataServiceFixture.CreateAsync(
            new ThrowAfterStageHook("clear", "operations"));
        Assert.True((await fixture.SeedAsync()).IsSuccess);

        var result = await fixture.ClearAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(4, await fixture.DbContext.InventoryTransfers.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(2, await fixture.DbContext.InventoryCounts.CountAsync(TestContext.Current.CancellationToken));
    }
}
