using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Application.Queries;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Catalog.Features.UnitsOfMeasure;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Catalog.Features.UnitsOfMeasure;

public sealed class UnitOfMeasureLifecycleHandlerTests
{
    [Fact]
    public async Task DeactivateHandleAsync_WhenUnitOfMeasureDoesNotExist_ReturnsNotFoundServiceResult()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        DeactivateUnitOfMeasure.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        DeactivateUnitOfMeasure.Command command = new(Guid.NewGuid());

        ServiceResult<UnitOfMeasureDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);

        Assert.Equal(ServiceErrorType.NotFound, result.Error.Type);
        Assert.Equal("UnitOfMeasure.NotFound", result.Error.Code);
        Assert.Equal("Unit of measure was not found.", result.Error.Message);
        Assert.Null(result.Error.Field);

        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task DeactivateHandleAsync_WhenUnitOfMeasureIsActive_DeactivatesAndHidesFromDefaultList()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        UnitOfMeasure unitOfMeasure = CreateUnitOfMeasure();

        testDbContext.DbContext.UnitsOfMeasure.Add(unitOfMeasure);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        DeactivateUnitOfMeasure.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        DeactivateUnitOfMeasure.Command command = new(unitOfMeasure.Id);

        ServiceResult<UnitOfMeasureDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);

        UnitOfMeasureDetails details = result.Value;

        Assert.Equal(unitOfMeasure.Id, details.Id);
        Assert.False(details.IsActive);
        Assert.NotNull(details.UpdatedAtUtc);

        UnitOfMeasure persistedUnitOfMeasure = await testDbContext.DbContext.UnitsOfMeasure.SingleAsync(
            TestContext.Current.CancellationToken);

        Assert.False(persistedUnitOfMeasure.IsActive);

        ListUnitsOfMeasure.Handler listHandler = new(testDbContext.DbContext);

        ServiceResult<ListResult<UnitOfMeasureDetails>> listResult = await listHandler.HandleAsync(
            new ListUnitsOfMeasure.Query(),
            TestContext.Current.CancellationToken);

        Assert.True(listResult.IsSuccess);
        Assert.Empty(listResult.Value.Items);

        var dispatchedEvent = Assert.Single(domainEventDispatcher.DispatchedEvents);
        Assert.IsType<UnitOfMeasureDeactivatedDomainEvent>(dispatchedEvent);
    }

    [Fact]
    public async Task DeactivateHandleAsync_WhenUnitOfMeasureIsAlreadyInactive_ReturnsSuccessWithoutDispatchingNewDomainEvent()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        UnitOfMeasure unitOfMeasure = CreateUnitOfMeasure();
        unitOfMeasure.Deactivate();
        unitOfMeasure.ClearDomainEvents();

        testDbContext.DbContext.UnitsOfMeasure.Add(unitOfMeasure);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        DeactivateUnitOfMeasure.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        DeactivateUnitOfMeasure.Command command = new(unitOfMeasure.Id);

        ServiceResult<UnitOfMeasureDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsActive);

        UnitOfMeasure persistedUnitOfMeasure = await testDbContext.DbContext.UnitsOfMeasure.SingleAsync(
            TestContext.Current.CancellationToken);

        Assert.False(persistedUnitOfMeasure.IsActive);
        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task ReactivateHandleAsync_WhenUnitOfMeasureDoesNotExist_ReturnsNotFoundServiceResult()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        ReactivateUnitOfMeasure.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        ReactivateUnitOfMeasure.Command command = new(Guid.NewGuid());

        ServiceResult<UnitOfMeasureDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);

        Assert.Equal(ServiceErrorType.NotFound, result.Error.Type);
        Assert.Equal("UnitOfMeasure.NotFound", result.Error.Code);
        Assert.Equal("Unit of measure was not found.", result.Error.Message);
        Assert.Null(result.Error.Field);

        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task ReactivateHandleAsync_WhenUnitOfMeasureIsInactive_ReactivatesAndReturnsToDefaultList()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        UnitOfMeasure unitOfMeasure = CreateUnitOfMeasure();
        unitOfMeasure.Deactivate();
        unitOfMeasure.ClearDomainEvents();

        testDbContext.DbContext.UnitsOfMeasure.Add(unitOfMeasure);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        ReactivateUnitOfMeasure.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        ReactivateUnitOfMeasure.Command command = new(unitOfMeasure.Id);

        ServiceResult<UnitOfMeasureDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);

        UnitOfMeasureDetails details = result.Value;

        Assert.Equal(unitOfMeasure.Id, details.Id);
        Assert.True(details.IsActive);
        Assert.NotNull(details.UpdatedAtUtc);

        UnitOfMeasure persistedUnitOfMeasure = await testDbContext.DbContext.UnitsOfMeasure.SingleAsync(
            TestContext.Current.CancellationToken);

        Assert.True(persistedUnitOfMeasure.IsActive);

        ListUnitsOfMeasure.Handler listHandler = new(testDbContext.DbContext);

        ServiceResult<ListResult<UnitOfMeasureDetails>> listResult = await listHandler.HandleAsync(
            new ListUnitsOfMeasure.Query(),
            TestContext.Current.CancellationToken);

        Assert.True(listResult.IsSuccess);
        UnitOfMeasureDetails listedDetails = Assert.Single(listResult.Value.Items);
        Assert.Equal(unitOfMeasure.Id, listedDetails.Id);

        var dispatchedEvent = Assert.Single(domainEventDispatcher.DispatchedEvents);
        Assert.IsType<UnitOfMeasureReactivatedDomainEvent>(dispatchedEvent);
    }

    [Fact]
    public async Task ReactivateHandleAsync_WhenUnitOfMeasureIsAlreadyActive_ReturnsSuccessWithoutDispatchingNewDomainEvent()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        UnitOfMeasure unitOfMeasure = CreateUnitOfMeasure();

        testDbContext.DbContext.UnitsOfMeasure.Add(unitOfMeasure);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        ReactivateUnitOfMeasure.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        ReactivateUnitOfMeasure.Command command = new(unitOfMeasure.Id);

        ServiceResult<UnitOfMeasureDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsActive);

        UnitOfMeasure persistedUnitOfMeasure = await testDbContext.DbContext.UnitsOfMeasure.SingleAsync(
            TestContext.Current.CancellationToken);

        Assert.True(persistedUnitOfMeasure.IsActive);
        Assert.Empty(domainEventDispatcher.DispatchedEvents);
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
