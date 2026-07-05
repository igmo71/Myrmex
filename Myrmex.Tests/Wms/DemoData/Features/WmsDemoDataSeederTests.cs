using Microsoft.EntityFrameworkCore;
using Myrmex.Tests.Wms.DemoData.Testing;

namespace Myrmex.Tests.Wms.DemoData.Features;

public sealed class WmsDemoDataSeederTests
{
    [Fact]
    public async Task SeedAsync_CreatesExactCatalogAndTopologyWithoutBarcodes()
    {
        await using DemoDataServiceFixture fixture = await DemoDataServiceFixture.CreateAsync();

        var result = await fixture.SeedAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(4, await fixture.DbContext.UnitsOfMeasure.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(10, await fixture.DbContext.StockKeepingUnits.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, await fixture.DbContext.Warehouses.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(7, await fixture.DbContext.Zones.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(15, await fixture.DbContext.StorageLocations.CountAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await fixture.DbContext.SkuBarcodes.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Contains(await fixture.DbContext.StockKeepingUnits.Select(x => x.Name)
            .ToListAsync(TestContext.Current.CancellationToken), x => x.Contains("Саморез"));
        Assert.Equal(4, result.Value.Areas.Single(x => x.Area == "unitsOfMeasure").Created);
    }
}
