using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Catalog.Features.UnitsOfMeasure;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Catalog.Features.UnitsOfMeasure;

public sealed class UpdateUnitOfMeasureDetailsHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenUnitOfMeasureDoesNotExist_ReturnsNotFoundServiceResult()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        UpdateUnitOfMeasureDetails.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        UpdateUnitOfMeasureDetails.Command command = new(
            UnitOfMeasureId: Guid.NewGuid(),
            Name: "Updated Each",
            Symbol: null);

        ServiceResult<UnitOfMeasureDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);

        Assert.Equal(ServiceErrorType.NotFound, result.Error.Type);
        Assert.Equal("UnitOfMeasure.NotFound", result.Error.Code);
        Assert.Equal("Unit of measure was not found.", result.Error.Message);
        Assert.Null(result.Error.Property);

        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenCommandIsInvalid_ReturnsInvalidServiceResult()
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
            Name: "",
            Symbol: null);

        ServiceResult<UnitOfMeasureDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);

        Assert.Equal(ServiceErrorType.Invalid, result.Error.Type);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Equal("One or more validation errors occurred.", result.Error.Message);

        var error = Assert.Single(result.Error.DetailList);

        Assert.Equal("UnitOfMeasure.NameRequired", error.Code);
        Assert.Equal("UoM name is required.", error.Message);
        Assert.Equal("name", error.Property);

        UnitOfMeasure persistedUnitOfMeasure = await testDbContext.DbContext.UnitsOfMeasure.SingleAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal("Each", persistedUnitOfMeasure.Name);
        Assert.Null(persistedUnitOfMeasure.Symbol);
        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }

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
