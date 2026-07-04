using Microsoft.EntityFrameworkCore;
using Myrmex.Tests.Wms.DemoData.Testing;

namespace Myrmex.Tests.Wms.DemoData.Features;

public sealed class WmsDemoDataIdempotencyTests
{
    [Fact]
    public async Task SeedAsync_Twice_DoesNotDuplicateStableDataOrEffects()
    {
        await using DemoDataServiceFixture fixture = await DemoDataServiceFixture.CreateAsync();
        Assert.True((await fixture.SeedAsync()).IsSuccess);
        int transactions = await fixture.DbContext.InventoryTransactions.CountAsync(TestContext.Current.CancellationToken);
        int movements = await fixture.DbContext.InventoryTransferMovements.CountAsync(TestContext.Current.CancellationToken);

        var second = await fixture.SeedAsync();

        Assert.True(second.IsSuccess);
        Assert.Equal(0, second.Value.Areas.Sum(x => x.Created));
        Assert.Equal(transactions, await fixture.DbContext.InventoryTransactions.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(movements, await fixture.DbContext.InventoryTransferMovements.CountAsync(TestContext.Current.CancellationToken));
        Assert.True(second.Value.Areas.Sum(x => x.Reused) > 0);
    }
}
