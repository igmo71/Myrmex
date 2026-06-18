using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Catalog.Features.StockKeepingUnits;
using Myrmex.Shared.Wms.Catalog;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Catalog.Features.StockKeepingUnits;

public sealed class LookupStockKeepingUnitsHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenSearchTextMatchesCodeOrName_ReturnsMatchingSkusOrderedByCode()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        await AddStockKeepingUnitAsync(testDbContext, "SKU-WIDGET", "Other");
        await AddStockKeepingUnitAsync(testDbContext, "BBB-001", "Widget");
        await AddStockKeepingUnitAsync(testDbContext, "CCC-001", "Other");

        LookupStockKeepingUnits.Handler handler = new(testDbContext.DbContext);

        ServiceResult<IReadOnlyList<StockKeepingUnitLookupItem>> result = await handler.HandleAsync(
            new LookupStockKeepingUnits.Query
            {
                SearchText = "Widget",
                SelectableOnly = true
            },
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(["BBB-001", "SKU-WIDGET"], result.Value.Select(x => x.Code).ToArray());
    }

    [Fact]
    public async Task HandleAsync_WhenTakeExceedsMaximum_ReturnsAtMostTwentyItems()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        for (int index = 1; index <= 25; index++)
        {
            await AddStockKeepingUnitAsync(
                testDbContext,
                $"ITEM-{index:000}",
                $"Item {index:000}");
        }

        LookupStockKeepingUnits.Handler handler = new(testDbContext.DbContext);

        ServiceResult<IReadOnlyList<StockKeepingUnitLookupItem>> result = await handler.HandleAsync(
            new LookupStockKeepingUnits.Query
            {
                Take = 1_000,
                SelectableOnly = true
            },
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(20, result.Value.Count);
        Assert.Equal("ITEM-001", result.Value[0].Code);
        Assert.Equal("ITEM-020", result.Value[^1].Code);
    }

    [Fact]
    public async Task HandleAsync_WhenSelectableOnlyIsTrue_ReturnsOnlyActiveSkusWithActiveBaseUnitOfMeasure()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        await AddStockKeepingUnitAsync(testDbContext, "ACTIVE", "Active");
        await AddStockKeepingUnitAsync(testDbContext, "INACTIVE-SKU", "Inactive SKU", isActive: false);
        await AddStockKeepingUnitAsync(testDbContext, "INACTIVE-UOM", "Inactive UoM", isBaseUnitOfMeasureActive: false);

        LookupStockKeepingUnits.Handler handler = new(testDbContext.DbContext);

        ServiceResult<IReadOnlyList<StockKeepingUnitLookupItem>> selectableResult = await handler.HandleAsync(
            new LookupStockKeepingUnits.Query
            {
                SelectableOnly = true
            },
            TestContext.Current.CancellationToken);

        ServiceResult<IReadOnlyList<StockKeepingUnitLookupItem>> filterResult = await handler.HandleAsync(
            new LookupStockKeepingUnits.Query
            {
                SelectableOnly = false
            },
            TestContext.Current.CancellationToken);

        Assert.True(selectableResult.IsSuccess);
        Assert.Equal(["ACTIVE"], selectableResult.Value.Select(x => x.Code).ToArray());

        Assert.True(filterResult.IsSuccess);
        Assert.Equal(["ACTIVE", "INACTIVE-SKU", "INACTIVE-UOM"], filterResult.Value.Select(x => x.Code).ToArray());
        Assert.Contains(filterResult.Value, x => x.Code == "INACTIVE-SKU" && !x.IsActive);
        Assert.Contains(filterResult.Value, x => x.Code == "INACTIVE-UOM" && !x.IsBaseUnitOfMeasureActive);
    }

    private static async Task AddStockKeepingUnitAsync(
        TestWmsDbContext testDbContext,
        string code,
        string name,
        bool isActive = true,
        bool isBaseUnitOfMeasureActive = true)
    {
        UnitOfMeasure unitOfMeasure = CreateUnitOfMeasure(code);

        if (!isBaseUnitOfMeasureActive)
        {
            unitOfMeasure.Deactivate();
        }

        testDbContext.DbContext.UnitsOfMeasure.Add(unitOfMeasure);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = StockKeepingUnit.Create(
            code,
            name,
            description: null,
            baseUnitOfMeasureId: unitOfMeasure.Id,
            out StockKeepingUnit? stockKeepingUnit);

        Assert.True(result.IsValid);
        Assert.NotNull(stockKeepingUnit);

        if (!isActive)
        {
            stockKeepingUnit.Deactivate();
        }

        stockKeepingUnit.ClearDomainEvents();
        testDbContext.DbContext.StockKeepingUnits.Add(stockKeepingUnit);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static UnitOfMeasure CreateUnitOfMeasure(string skuCode)
    {
        string unitCode = skuCode.Replace("-", string.Empty);

        var result = UnitOfMeasure.Create(
            unitCode,
            unitCode,
            unitCode.ToLowerInvariant(),
            out UnitOfMeasure? unitOfMeasure);

        Assert.True(result.IsValid);
        Assert.NotNull(unitOfMeasure);

        unitOfMeasure.ClearDomainEvents();

        return unitOfMeasure;
    }
}
