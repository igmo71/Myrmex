using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Catalog.Features.UnitsOfMeasure;
using Myrmex.Shared.Wms.Catalog;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Catalog.Features.UnitsOfMeasure;

public sealed class UpdateUnitOfMeasureDetailsHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenCommandIsValid_UpdatesUnitOfMeasureAndReturnsDetails()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        UnitOfMeasure unitOfMeasure = CreateUnitOfMeasure();

        testDbContext.DbContext.UnitsOfMeasure.Add(unitOfMeasure);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        UpdateUnitOfMeasureDetails.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        UpdateUnitOfMeasureDetails.Command command = new(
            UnitOfMeasureId: unitOfMeasure.Id,
            Name: " Updated Each ",
            Symbol: " each ");

        ServiceResult<UnitOfMeasureDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);

        UnitOfMeasureDetails details = result.Value;

        Assert.Equal(unitOfMeasure.Id, details.Id);
        Assert.Equal("EA", details.Code);
        Assert.Equal("Updated Each", details.Name);
        Assert.Equal("each", details.Symbol);
        Assert.True(details.IsActive);
        Assert.NotNull(details.UpdatedAtUtc);

        UnitOfMeasure persistedUnitOfMeasure = await testDbContext.DbContext.UnitsOfMeasure.SingleAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal("EA", persistedUnitOfMeasure.Code);
        Assert.Equal("Updated Each", persistedUnitOfMeasure.Name);
        Assert.Equal("each", persistedUnitOfMeasure.Symbol);

        var dispatchedEvent = Assert.Single(domainEventDispatcher.DispatchedEvents);
        Assert.IsType<UnitOfMeasureDetailsUpdatedDomainEvent>(dispatchedEvent);
    }

    private static UnitOfMeasure CreateUnitOfMeasure()
    {
        var result = UnitOfMeasure.Create(
            code: "EA",
            name: "Each",
            symbol: null,
            out UnitOfMeasure? unitOfMeasure);

        Assert.True(result.IsValid);
        Assert.NotNull(unitOfMeasure);

        unitOfMeasure.ClearDomainEvents();

        return unitOfMeasure;
    }
}
