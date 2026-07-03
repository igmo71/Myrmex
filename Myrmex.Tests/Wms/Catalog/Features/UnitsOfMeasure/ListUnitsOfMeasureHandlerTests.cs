using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Catalog.Features.UnitsOfMeasure;
using Myrmex.Shared.Common;
using Myrmex.Tests.Wms.Topology.Testing;
using System.Data.SqlTypes;

namespace Myrmex.Tests.Wms.Catalog.Features.UnitsOfMeasure;

public sealed class ListUnitsOfMeasureHandlerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HandleAsync_WhenNameValuesMatch_OrdersByIdAcrossPages(bool sortDescending)
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        string marker = Guid.NewGuid().ToString("N")[..8];
        UnitOfMeasure[] unitsOfMeasure =
        [
            await AddUnitOfMeasureAsync(testDbContext, $"UOM-{marker}-A", $"Matching {marker}", $"{marker}a"),
            await AddUnitOfMeasureAsync(testDbContext, $"UOM-{marker}-B", $"Matching {marker}", $"{marker}b"),
            await AddUnitOfMeasureAsync(testDbContext, $"UOM-{marker}-C", $"Matching {marker}", $"{marker}c")
        ];

        ListUnitsOfMeasure.Handler handler = new(testDbContext.DbContext);

        ListResult<UnitOfMeasureDetails> firstPage = await GetPageAsync(handler, marker, sortDescending, skip: 0);
        ListResult<UnitOfMeasureDetails> secondPage = await GetPageAsync(handler, marker, sortDescending, skip: 2);

        Guid[] expectedIds = unitsOfMeasure
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
    public async Task HandleAsync_WhenIncludeInactiveIsFalse_ReturnsActiveUnitsOfMeasureOnly()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        await AddUnitOfMeasureAsync(testDbContext, "EA", "Each", "ea", isActive: true);
        await AddUnitOfMeasureAsync(testDbContext, "CS", "Case", "cs", isActive: false);

        ListUnitsOfMeasure.Handler handler = new(testDbContext.DbContext);

        // Act
        ServiceResult<ListResult<UnitOfMeasureDetails>> result = await handler.HandleAsync(
            new ListUnitsOfMeasure.Query(),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalCount);

        UnitOfMeasureDetails details = Assert.Single(result.Value.Items);
        Assert.Equal("EA", details.Code);
        Assert.True(details.IsActive);
    }

    [Fact]
    public async Task HandleAsync_WhenIncludeInactiveIsTrue_ReturnsActiveAndInactiveUnitsOfMeasure()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        await AddUnitOfMeasureAsync(testDbContext, "CS", "Case", "cs", isActive: false);
        await AddUnitOfMeasureAsync(testDbContext, "EA", "Each", "ea", isActive: true);

        ListUnitsOfMeasure.Handler handler = new(testDbContext.DbContext);

        var query = new ListUnitsOfMeasure.Query
        {
            IncludeInactive = true
        };

        // Act
        ServiceResult<ListResult<UnitOfMeasureDetails>> result = await handler.HandleAsync(
            query,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Equal(["CS", "EA"], result.Value.Items.Select(x => x.Code).ToArray());
    }

    [Fact]
    public async Task HandleAsync_WhenSearchTextMatchesCodeNameOrSymbol_ReturnsMatchingUnitsOfMeasure()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        await AddUnitOfMeasureAsync(testDbContext, "EA", "Each", "ea");
        await AddUnitOfMeasureAsync(testDbContext, "BOX", "Box", "Each-box");
        await AddUnitOfMeasureAsync(testDbContext, "PAL", "Pallet", "plt");

        ListUnitsOfMeasure.Handler handler = new(testDbContext.DbContext);

        var query = new ListUnitsOfMeasure.Query
        {
            SearchText = "Each"
        };

        // Act
        ServiceResult<ListResult<UnitOfMeasureDetails>> result = await handler.HandleAsync(
            query,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Equal(["BOX", "EA"], result.Value.Items.Select(x => x.Code).ToArray());
    }

    [Theory]
    [InlineData("code")]
    [InlineData("name")]
    [InlineData("isActive")]
    public async Task HandleAsync_WhenSortByIsSupported_SortsByRequestedField(string sortBy)
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        await AddUnitOfMeasureAsync(testDbContext, "A-UOM", "Alpha", "a", isActive: false);
        await AddUnitOfMeasureAsync(testDbContext, "B-UOM", "Beta", "b", isActive: true);

        ListUnitsOfMeasure.Handler handler = new(testDbContext.DbContext);

        var query = new ListUnitsOfMeasure.Query
        {
            SortBy = sortBy,
            IncludeInactive = true
        };

        // Act
        ServiceResult<ListResult<UnitOfMeasureDetails>> result = await handler.HandleAsync(
            query,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(["A-UOM", "B-UOM"], result.Value.Items.Select(x => x.Code).ToArray());
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("createdAtUtc")]
    [InlineData("updatedAtUtc")]
    public async Task HandleAsync_WhenSortByIsUnsupported_FallsBackToCodeOrdering(string sortBy)
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        await AddUnitOfMeasureAsync(testDbContext, "B-UOM", "Beta", "b");
        await AddUnitOfMeasureAsync(testDbContext, "A-UOM", "Alpha", "a");

        ListUnitsOfMeasure.Handler handler = new(testDbContext.DbContext);

        var query = new ListUnitsOfMeasure.Query
        {
            SortBy = sortBy,
            SortDescending = true
        };

        // Act
        ServiceResult<ListResult<UnitOfMeasureDetails>> result = await handler.HandleAsync(
            query,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(["A-UOM", "B-UOM"], result.Value.Items.Select(x => x.Code).ToArray());
    }

    private static async Task<UnitOfMeasure> AddUnitOfMeasureAsync(
        TestWmsDbContext testDbContext,
        string code,
        string name,
        string? symbol,
        bool isActive = true)
    {
        var result = UnitOfMeasure.Create(
            code,
            name,
            symbol,
            out UnitOfMeasure? unitOfMeasure);

        Assert.True(result.IsValid);
        Assert.NotNull(unitOfMeasure);

        unitOfMeasure.ClearDomainEvents();
        testDbContext.DbContext.UnitsOfMeasure.Add(unitOfMeasure);

        testDbContext.DbContext.Entry(unitOfMeasure)
            .Property(nameof(UnitOfMeasure.IsActive))
            .CurrentValue = isActive;

        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return unitOfMeasure;
    }

    private static async Task<ListResult<UnitOfMeasureDetails>> GetPageAsync(
        ListUnitsOfMeasure.Handler handler,
        string marker,
        bool sortDescending,
        int skip)
    {
        ServiceResult<ListResult<UnitOfMeasureDetails>> result = await handler.HandleAsync(
            new ListUnitsOfMeasure.Query
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
}
