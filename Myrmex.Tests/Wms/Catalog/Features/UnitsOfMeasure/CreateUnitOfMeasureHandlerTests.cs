using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Catalog.Features.UnitsOfMeasure;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Catalog.Features.UnitsOfMeasure;

public sealed class CreateUnitOfMeasureHandlerTests
{

    [Fact]
    public async Task HandleAsync_WhenCommandIsValid_CreatesUomAndReturnsDetails()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        CreateUnitOfMeasure.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        CreateUnitOfMeasure.Command command = new(
            Code: " ea ",
            Name: " Each ",
            Symbol: " ea ");

        // Act
        ServiceResult<UnitOfMeasureDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);

        UnitOfMeasureDetails details = result.Value;

        Assert.NotEqual(Guid.Empty, details.Id);
        Assert.Equal("EA", details.Code);
        Assert.Equal("Each", details.Name);
        Assert.Equal("ea", details.Symbol);
        Assert.True(details.IsActive);
        Assert.Null(details.UpdatedAtUtc);

        var unitOfMeasure = await testDbContext.DbContext.UnitsOfMeasure.SingleAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(details.Id, unitOfMeasure.Id);
        Assert.Equal("EA", unitOfMeasure.Code);
        Assert.Equal("Each", unitOfMeasure.Name);
        Assert.Equal("ea", unitOfMeasure.Symbol);
        Assert.True(unitOfMeasure.IsActive);
        Assert.Null(unitOfMeasure.UpdatedAtUtc);

        var createdEvent = Assert.Single(domainEventDispatcher.DispatchedEvents);
        Assert.IsType<UnitOfMeasureCreatedDomainEvent>(createdEvent);
    }
}
