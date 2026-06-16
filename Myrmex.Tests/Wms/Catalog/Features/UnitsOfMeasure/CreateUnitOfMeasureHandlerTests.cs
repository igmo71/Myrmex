using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Catalog.Features.UnitsOfMeasure;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Catalog.Features.UnitsOfMeasure;

public sealed class CreateUnitOfMeasureHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenCommandIsInvalid_ReturnsInvalidServiceResult()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        CreateUnitOfMeasure.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        CreateUnitOfMeasure.Command command = new(
            Code: "",
            Name: "",
            Symbol: null);

        // Act
        ServiceResult<UnitOfMeasureDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);

        Assert.Equal(ServiceErrorType.Invalid, result.Error.Type);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Equal("One or more validation errors occurred.", result.Error.Message);

        Assert.Contains(result.Error.DetailList, error =>
            error.Code == "UnitOfMeasure.CodeRequired" &&
            error.Property == "code");

        Assert.Contains(result.Error.DetailList, error =>
            error.Code == "UnitOfMeasure.NameRequired" &&
            error.Property == "name");

        Assert.Empty(await testDbContext.DbContext.UnitsOfMeasure.ToListAsync(
            TestContext.Current.CancellationToken));

        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenCodeAlreadyExists_ReturnsConflictServiceResult()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        CreateUnitOfMeasure.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        CreateUnitOfMeasure.Command firstCommand = new(
            Code: "EA",
            Name: "Each",
            Symbol: "ea");

        ServiceResult<UnitOfMeasureDetails> firstResult = await handler.HandleAsync(
            firstCommand,
            TestContext.Current.CancellationToken);

        Assert.True(firstResult.IsSuccess);

        CreateUnitOfMeasure.Command duplicateCommand = new(
            Code: " ea ",
            Name: "Each duplicate",
            Symbol: null);

        // Act
        ServiceResult<UnitOfMeasureDetails> result = await handler.HandleAsync(
            duplicateCommand,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);

        Assert.Equal(ServiceErrorType.Conflict, result.Error.Type);
        Assert.Equal("UnitOfMeasure.CodeAlreadyExists", result.Error.Code);
        Assert.Equal("Unit of measure with the same code already exists.", result.Error.Message);
        Assert.Equal("code", result.Error.Property);

        int unitOfMeasureCount = await testDbContext.DbContext.UnitsOfMeasure.CountAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(1, unitOfMeasureCount);
    }

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
