using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;
using Myrmex.Modules.Wms.Inventory.Features.InventoryBalances;
using Myrmex.Tests.Wms.Inventory.Testing;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Inventory.Features.InventoryBalances;

public sealed class UpdateInventoryBalanceQuantityHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenQuantityIsZero_UpdatesQuantityToZero()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();
        SeededInventoryBalance seeded = await InventoryBalanceTestData.SeedInventoryBalanceAsync(
            testDbContext.DbContext,
            quantity: 10);
        UpdateInventoryBalanceQuantity.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        ServiceResult<InventoryBalanceDetails> result = await handler.HandleAsync(
            new UpdateInventoryBalanceQuantity.Command(seeded.InventoryBalance.Id, Quantity: 0),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.Quantity);
        Assert.NotNull(result.Value.UpdatedAtUtc);
    }

    [Fact]
    public async Task HandleAsync_WhenInventoryBalanceDoesNotExist_ReturnsNotFound()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();
        UpdateInventoryBalanceQuantity.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        ServiceResult<InventoryBalanceDetails> result = await handler.HandleAsync(
            new UpdateInventoryBalanceQuantity.Command(Guid.NewGuid(), Quantity: 5),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal(ServiceErrorType.NotFound, result.Error.Type);
        Assert.Equal("InventoryBalance.NotFound", result.Error.Code);
        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenQuantityIsNegative_ReturnsValidationAndKeepsBalanceUnchanged()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();
        SeededInventoryBalance seeded = await InventoryBalanceTestData.SeedInventoryBalanceAsync(
            testDbContext.DbContext,
            quantity: 10);
        Guid originalStockKeepingUnitId = seeded.InventoryBalance.StockKeepingUnitId;
        Guid originalStorageLocationId = seeded.InventoryBalance.StorageLocationId;
        UpdateInventoryBalanceQuantity.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        ServiceResult<InventoryBalanceDetails> result = await handler.HandleAsync(
            new UpdateInventoryBalanceQuantity.Command(seeded.InventoryBalance.Id, Quantity: -1),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal(ServiceErrorType.Invalid, result.Error.Type);
        Assert.Equal("Validation.Invalid", result.Error.Code);

        Assert.Contains(result.Error.DetailList, error =>
            error.Code == "InventoryBalance.QuantityMustBeNonNegative" &&
            error.Field == "quantity");

        InventoryBalance storedBalance = await testDbContext.DbContext.InventoryBalances.SingleAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(10, storedBalance.Quantity);
        Assert.Equal(originalStockKeepingUnitId, storedBalance.StockKeepingUnitId);
        Assert.Equal(originalStorageLocationId, storedBalance.StorageLocationId);
        Assert.Null(storedBalance.UpdatedAtUtc);
        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }
}
