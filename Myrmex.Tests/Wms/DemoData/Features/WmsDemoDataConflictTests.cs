using Myrmex.Core.Domain.Validation;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Tests.Wms.DemoData.Testing;

namespace Myrmex.Tests.Wms.DemoData.Features;

public sealed class WmsDemoDataConflictTests
{
    [Fact]
    public async Task SeedAsync_WithIncompatibleStableIdentity_ReturnsConflictWithoutOverwrite()
    {
        await using DemoDataServiceFixture fixture = await DemoDataServiceFixture.CreateAsync();
        DomainValidationResult validation = UnitOfMeasure.Create("PCS", "Pieces", "pc", out UnitOfMeasure? unit);
        Assert.True(validation.IsValid);
        fixture.DbContext.UnitsOfMeasure.Add(unit!);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await fixture.SeedAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal("DemoData.IdentityConflict", result.Error.Code);
        Assert.Equal("Pieces", fixture.DbContext.UnitsOfMeasure.Single().Name);
        Assert.Empty(fixture.DbContext.Warehouses);
    }
}
