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

    [Theory]
    [InlineData("Changed name", null)]
    [InlineData("Each", "changed")]
    public async Task HandleAsync_WhenLinkedSourceOwnedValueChanges_RejectsTheChange(
        string name,
        string? symbol)
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();
        UnitOfMeasure unitOfMeasure = CreateLinkedUnitOfMeasure();
        testDbContext.DbContext.UnitsOfMeasure.Add(unitOfMeasure);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        UpdateUnitOfMeasureDetails.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        ServiceResult<UnitOfMeasureDetails> result = await handler.HandleAsync(
            new UpdateUnitOfMeasureDetails.Command(unitOfMeasure.Id, name, symbol),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorType.Invalid, result.Error.Type);
        Assert.Equal("Each", unitOfMeasure.Name);
        Assert.Null(unitOfMeasure.Symbol);
        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenLinkedValuesAreResubmittedIdentically_ReturnsNoOp()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();
        UnitOfMeasure unitOfMeasure = CreateLinkedUnitOfMeasure();
        DateTimeOffset? updatedAtUtc = unitOfMeasure.UpdatedAtUtc;
        testDbContext.DbContext.UnitsOfMeasure.Add(unitOfMeasure);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        UpdateUnitOfMeasureDetails.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        ServiceResult<UnitOfMeasureDetails> result = await handler.HandleAsync(
            new UpdateUnitOfMeasureDetails.Command(
                unitOfMeasure.Id,
                Name: " Each ",
                Symbol: null),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(updatedAtUtc, unitOfMeasure.UpdatedAtUtc);
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

    private static UnitOfMeasure CreateLinkedUnitOfMeasure()
    {
        UnitOfMeasure unitOfMeasure = CreateUnitOfMeasure();
        var result = unitOfMeasure.ApplyImport(
            Guid.NewGuid(),
            [1],
            unitOfMeasure.Code,
            unitOfMeasure.Name,
            unitOfMeasure.Symbol,
            isDeletionMarked: false,
            importedAtUtc: DateTimeOffset.Parse("2026-07-17T12:00:00Z"));
        Assert.True(result.IsValid);
        unitOfMeasure.ClearDomainEvents();
        return unitOfMeasure;
    }
}
