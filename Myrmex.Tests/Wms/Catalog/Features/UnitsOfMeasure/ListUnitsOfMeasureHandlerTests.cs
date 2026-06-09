using Myrmex.Core.Application.Queries;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Catalog.Features.UnitsOfMeasure;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Catalog.Features.UnitsOfMeasure;

public sealed class ListUnitsOfMeasureHandlerTests
{
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

    private static async Task AddUnitOfMeasureAsync(
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
    }
}
