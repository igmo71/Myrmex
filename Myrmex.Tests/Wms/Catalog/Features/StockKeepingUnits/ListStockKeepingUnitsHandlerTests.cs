using Myrmex.Core.Application.Queries;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Catalog.Features.StockKeepingUnits;
using Myrmex.Shared.Common;
using Myrmex.Tests.Wms.Topology.Testing;
using System.Data.SqlTypes;

namespace Myrmex.Tests.Wms.Catalog.Features.StockKeepingUnits;

public sealed class ListStockKeepingUnitsHandlerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HandleAsync_WhenNameValuesMatch_OrdersByIdAcrossPages(bool sortDescending)
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        string marker = Guid.NewGuid().ToString("N")[..8];
        StockKeepingUnit[] stockKeepingUnits =
        [
            await AddStockKeepingUnitAsync(testDbContext, $"SKU-{marker}-A", $"Matching {marker}"),
            await AddStockKeepingUnitAsync(testDbContext, $"SKU-{marker}-B", $"Matching {marker}"),
            await AddStockKeepingUnitAsync(testDbContext, $"SKU-{marker}-C", $"Matching {marker}")
        ];

        ListStockKeepingUnits.Handler handler = new(testDbContext.DbContext);

        ListResult<StockKeepingUnitDetails> firstPage = await GetPageAsync(handler, marker, sortDescending, skip: 0);
        ListResult<StockKeepingUnitDetails> secondPage = await GetPageAsync(handler, marker, sortDescending, skip: 2);

        Guid[] expectedIds = stockKeepingUnits
            .OrderBy(x => new SqlGuid(x.Id))
            .Select(x => x.Id)
            .ToArray();
        Guid[] actualIds = firstPage.Items
            .Concat(secondPage.Items)
            .Select(x => x.Id)
            .ToArray();

        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(3, secondPage.TotalCount);
        Assert.Equal(expectedIds, actualIds);
    }

    [Fact]
    public async Task HandleAsync_WhenIncludeInactiveIsFalse_ReturnsActiveStockKeepingUnitsOnly()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        StockKeepingUnit activeStockKeepingUnit = await AddStockKeepingUnitAsync(
            testDbContext,
            "ITEM-001",
            "Widget",
            isActive: true);

        await AddStockKeepingUnitAsync(testDbContext, "ITEM-002", "Inactive Widget", isActive: false);

        ListStockKeepingUnits.Handler handler = new(testDbContext.DbContext);

        // Act
        ServiceResult<ListResult<StockKeepingUnitDetails>> result = await handler.HandleAsync(
            new ListStockKeepingUnits.Query(),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalCount);

        StockKeepingUnitDetails details = Assert.Single(result.Value.Items);
        Assert.Equal("ITEM-001", details.Code);
        Assert.Equal(activeStockKeepingUnit.BaseUnitOfMeasureId, details.BaseUnitOfMeasureId);
        Assert.True(details.IsActive);
    }

    [Fact]
    public async Task HandleAsync_WhenIncludeInactiveIsTrue_ReturnsActiveAndInactiveStockKeepingUnits()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        StockKeepingUnit activeStockKeepingUnit = await AddStockKeepingUnitAsync(
            testDbContext,
            "ITEM-001",
            "Widget",
            isActive: true);

        StockKeepingUnit inactiveStockKeepingUnit = await AddStockKeepingUnitAsync(
            testDbContext,
            "ITEM-002",
            "Inactive Widget",
            isActive: false);

        ListStockKeepingUnits.Handler handler = new(testDbContext.DbContext);

        var query = new ListStockKeepingUnits.Query
        {
            IncludeInactive = true
        };

        // Act
        ServiceResult<ListResult<StockKeepingUnitDetails>> result = await handler.HandleAsync(
            query,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Equal(["ITEM-001", "ITEM-002"], result.Value.Items.Select(x => x.Code).ToArray());

        Dictionary<string, Guid> expectedBaseUnitOfMeasureIds = new()
        {
            [activeStockKeepingUnit.Code] = activeStockKeepingUnit.BaseUnitOfMeasureId,
            [inactiveStockKeepingUnit.Code] = inactiveStockKeepingUnit.BaseUnitOfMeasureId
        };

        Assert.All(result.Value.Items, details =>
            Assert.Equal(expectedBaseUnitOfMeasureIds[details.Code], details.BaseUnitOfMeasureId));
    }

    [Fact]
    public async Task HandleAsync_WhenSearchTextMatchesCodeNameOrDescription_ReturnsMatchingStockKeepingUnits()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        await AddStockKeepingUnitAsync(testDbContext, "ITEM-001", "Widget", "Primary item");
        await AddStockKeepingUnitAsync(testDbContext, "PART-002", "Bracket", "Widget compatible part");
        await AddStockKeepingUnitAsync(testDbContext, "OTHER-003", "Cable", "Cord");

        ListStockKeepingUnits.Handler handler = new(testDbContext.DbContext);

        var query = new ListStockKeepingUnits.Query
        {
            SearchText = "Widget"
        };

        // Act
        ServiceResult<ListResult<StockKeepingUnitDetails>> result = await handler.HandleAsync(
            query,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Equal(["ITEM-001", "PART-002"], result.Value.Items.Select(x => x.Code).ToArray());
    }

    [Fact]
    public async Task HandleAsync_WhenPagingValuesAreOutOfRange_NormalizesSkipAndTake()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        await AddStockKeepingUnitAsync(testDbContext, "ITEM-001", "Widget 1");
        await AddStockKeepingUnitAsync(testDbContext, "ITEM-002", "Widget 2");
        await AddStockKeepingUnitAsync(testDbContext, "ITEM-003", "Widget 3");

        ListStockKeepingUnits.Handler handler = new(testDbContext.DbContext);

        var query = new ListStockKeepingUnits.Query
        {
            Skip = -5,
            Take = 1_000
        };

        // Act
        ServiceResult<ListResult<StockKeepingUnitDetails>> result = await handler.HandleAsync(
            query,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.TotalCount);
        Assert.Equal(0, result.Value.Skip);
        Assert.Equal(ListQuery.MaxTake, result.Value.Take);
        Assert.Equal(3, result.Value.Items.Count);
    }

    [Theory]
    [InlineData("code")]
    [InlineData("name")]
    [InlineData("isActive")]
    public async Task HandleAsync_WhenSortByIsSupported_SortsByRequestedField(string sortBy)
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        await AddStockKeepingUnitAsync(
            testDbContext,
            "ITEM-A",
            "Alpha",
            isActive: false);

        await AddStockKeepingUnitAsync(
            testDbContext,
            "ITEM-B",
            "Beta",
            isActive: true);

        ListStockKeepingUnits.Handler handler = new(testDbContext.DbContext);

        var query = new ListStockKeepingUnits.Query
        {
            SortBy = sortBy,
            IncludeInactive = true
        };

        // Act
        ServiceResult<ListResult<StockKeepingUnitDetails>> result = await handler.HandleAsync(
            query,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(["ITEM-A", "ITEM-B"], result.Value.Items.Select(x => x.Code).ToArray());
    }

    [Fact]
    public async Task HandleAsync_WhenSortDescendingIsTrue_SortsDescending()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        await AddStockKeepingUnitAsync(testDbContext, "ITEM-A", "Alpha");
        await AddStockKeepingUnitAsync(testDbContext, "ITEM-B", "Beta");

        ListStockKeepingUnits.Handler handler = new(testDbContext.DbContext);

        var query = new ListStockKeepingUnits.Query
        {
            SortBy = "code",
            SortDescending = true
        };

        // Act
        ServiceResult<ListResult<StockKeepingUnitDetails>> result = await handler.HandleAsync(
            query,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(["ITEM-B", "ITEM-A"], result.Value.Items.Select(x => x.Code).ToArray());
    }

    [Fact]
    public async Task HandleAsync_WhenSortByIsUnsupported_FallsBackToCodeOrdering()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        await AddStockKeepingUnitAsync(testDbContext, "ITEM-B", "Beta");
        await AddStockKeepingUnitAsync(testDbContext, "ITEM-A", "Alpha");

        ListStockKeepingUnits.Handler handler = new(testDbContext.DbContext);

        var query = new ListStockKeepingUnits.Query
        {
            SortBy = "unknown",
            SortDescending = true
        };

        // Act
        ServiceResult<ListResult<StockKeepingUnitDetails>> result = await handler.HandleAsync(
            query,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(["ITEM-A", "ITEM-B"], result.Value.Items.Select(x => x.Code).ToArray());
    }

    private static async Task<StockKeepingUnit> AddStockKeepingUnitAsync(
        TestWmsDbContext testDbContext,
        string code,
        string name,
        string? description = null,
        bool isActive = true)
    {
        UnitOfMeasure baseUnitOfMeasure = CreateUnitOfMeasure(code);
        testDbContext.DbContext.UnitsOfMeasure.Add(baseUnitOfMeasure);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = StockKeepingUnit.Create(
            code,
            name,
            description,
            baseUnitOfMeasureId: baseUnitOfMeasure.Id,
            out StockKeepingUnit? stockKeepingUnit);

        Assert.True(result.IsValid);
        Assert.NotNull(stockKeepingUnit);

        stockKeepingUnit.ClearDomainEvents();
        testDbContext.DbContext.StockKeepingUnits.Add(stockKeepingUnit);

        testDbContext.DbContext.Entry(stockKeepingUnit)
            .Property(nameof(StockKeepingUnit.IsActive))
            .CurrentValue = isActive;

        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return stockKeepingUnit;
    }

    private static async Task<ListResult<StockKeepingUnitDetails>> GetPageAsync(
        ListStockKeepingUnits.Handler handler,
        string marker,
        bool sortDescending,
        int skip)
    {
        ServiceResult<ListResult<StockKeepingUnitDetails>> result = await handler.HandleAsync(
            new ListStockKeepingUnits.Query
            {
                SearchText = marker,
                SortBy = "name",
                SortDescending = sortDescending,
                Skip = skip,
                Take = 2
            },
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private static UnitOfMeasure CreateUnitOfMeasure(string skuCode)
    {
        string unitCode = skuCode.Replace("-", string.Empty);

        var result = UnitOfMeasure.Create(
            code: unitCode,
            name: unitCode,
            symbol: unitCode.ToLowerInvariant(),
            out UnitOfMeasure? unitOfMeasure);

        Assert.True(result.IsValid);
        Assert.NotNull(unitOfMeasure);

        unitOfMeasure.ClearDomainEvents();

        return unitOfMeasure;
    }
}
