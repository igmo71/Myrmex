using Microsoft.EntityFrameworkCore;
using Myrmex.Tests.Wms.DemoData.Testing;

namespace Myrmex.Tests.Wms.DemoData.Features;

public sealed class WmsDemoDataResetTests
{
    [Fact]
    public async Task SeedClearReseed_RestoresEquivalentBoundedScenario()
    {
        await using DemoDataServiceFixture fixture = await DemoDataServiceFixture.CreateAsync();
        Assert.True((await fixture.SeedAsync()).IsSuccess);
        Assert.True((await fixture.ClearAsync()).IsSuccess);

        var reseed = await fixture.SeedAsync();

        Assert.True(reseed.IsSuccess);
        Assert.Equal(10, await fixture.DbContext.StockKeepingUnits.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(4, await fixture.DbContext.InventoryTransfers.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(2, await fixture.DbContext.InventoryCounts.CountAsync(TestContext.Current.CancellationToken));
    }
}
