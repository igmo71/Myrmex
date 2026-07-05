using Myrmex.Core.Domain.Validation;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Tests.Wms.DemoData.Testing;

namespace Myrmex.Tests.Wms.DemoData.Features;

public sealed class WmsDemoDataResumeTests
{
    [Fact]
    public async Task SeedAsync_WithCompatibleReference_ResumesRemainingStages()
    {
        await using DemoDataServiceFixture fixture = await DemoDataServiceFixture.CreateAsync();
        DomainValidationResult validation = UnitOfMeasure.Create("PCS", "Штука", "шт", out UnitOfMeasure? unit);
        Assert.True(validation.IsValid);
        fixture.DbContext.UnitsOfMeasure.Add(unit!);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await fixture.SeedAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Areas.Single(x => x.Area == "unitsOfMeasure").Reused);
        Assert.Equal(4, fixture.DbContext.UnitsOfMeasure.Count());
        Assert.Equal(4, fixture.DbContext.InventoryTransfers.Count());
    }
}
