using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Catalog.Features.UnitsOfMeasure;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Catalog.Features.UnitsOfMeasure;

public sealed class GetUnitOfMeasureByIdHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenUnitOfMeasureIsActive_ReturnsDetails()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        UnitOfMeasure unitOfMeasure = await AddUnitOfMeasureAsync(
            testDbContext,
            "EA",
            "Each",
            isActive: true);

        GetUnitOfMeasureById.Handler handler = new(testDbContext.DbContext);

        // Act
        ServiceResult<UnitOfMeasureDetails> result = await handler.HandleAsync(
            new GetUnitOfMeasureById.Query(unitOfMeasure.Id),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(unitOfMeasure.Id, result.Value.Id);
        Assert.Equal("EA", result.Value.Code);
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public async Task HandleAsync_WhenUnitOfMeasureIsInactive_ReturnsDetails()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        UnitOfMeasure unitOfMeasure = await AddUnitOfMeasureAsync(
            testDbContext,
            "EA",
            "Each",
            isActive: false);

        GetUnitOfMeasureById.Handler handler = new(testDbContext.DbContext);

        // Act
        ServiceResult<UnitOfMeasureDetails> result = await handler.HandleAsync(
            new GetUnitOfMeasureById.Query(unitOfMeasure.Id),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(unitOfMeasure.Id, result.Value.Id);
        Assert.Equal("EA", result.Value.Code);
        Assert.False(result.Value.IsActive);
    }

    private static async Task<UnitOfMeasure> AddUnitOfMeasureAsync(
        TestWmsDbContext testDbContext,
        string code,
        string name,
        bool isActive)
    {
        var result = UnitOfMeasure.Create(
            code,
            name,
            symbol: null,
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
}
