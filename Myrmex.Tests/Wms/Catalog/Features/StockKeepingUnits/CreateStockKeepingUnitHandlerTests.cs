using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Catalog.Features.StockKeepingUnits;
using Myrmex.Shared.Wms.Catalog;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Catalog.Features.StockKeepingUnits;

public sealed class CreateStockKeepingUnitHandlerTests
{

    [Fact]
    public async Task HandleAsync_WhenCommandIsValid_CreatesSkuAndReturnsDetails()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        CreateStockKeepingUnit.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        UnitOfMeasure baseUnitOfMeasure = CreateUnitOfMeasure();

        testDbContext.DbContext.UnitsOfMeasure.Add(baseUnitOfMeasure);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        CreateStockKeepingUnit.Command command = new(
            Code: " item-001 ",
            Name: " Widget ",
            Description: " Sellable widget ",
            BaseUnitOfMeasureId: baseUnitOfMeasure.Id);

        // Act
        ServiceResult<StockKeepingUnitDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);

        StockKeepingUnitDetails details = result.Value;

        Assert.NotEqual(Guid.Empty, details.Id);
        Assert.Equal("ITEM-001", details.Code);
        Assert.Equal("Widget", details.Name);
        Assert.Equal("Sellable widget", details.Description);
        Assert.Equal(baseUnitOfMeasure.Id, details.BaseUnitOfMeasureId);
        Assert.True(details.IsActive);
        Assert.Null(details.UpdatedAtUtc);

        var stockKeepingUnit = await testDbContext.DbContext.StockKeepingUnits.SingleAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(details.Id, stockKeepingUnit.Id);
        Assert.Equal("ITEM-001", stockKeepingUnit.Code);
        Assert.Equal("Widget", stockKeepingUnit.Name);
        Assert.Equal("Sellable widget", stockKeepingUnit.Description);
        Assert.Equal(baseUnitOfMeasure.Id, stockKeepingUnit.BaseUnitOfMeasureId);
        Assert.True(stockKeepingUnit.IsActive);
        Assert.Null(stockKeepingUnit.UpdatedAtUtc);

        var createdEvent = Assert.Single(domainEventDispatcher.DispatchedEvents);
        Assert.IsType<StockKeepingUnitCreatedDomainEvent>(createdEvent);
    }

    private static UnitOfMeasure CreateUnitOfMeasure()
    {
        var result = UnitOfMeasure.Create(
            code: "EA",
            name: "Each",
            symbol: "ea",
            out UnitOfMeasure? unitOfMeasure);

        Assert.True(result.IsValid);
        Assert.NotNull(unitOfMeasure);

        unitOfMeasure.ClearDomainEvents();

        return unitOfMeasure;
    }
}
