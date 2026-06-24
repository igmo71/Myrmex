using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Inventory.Features.InventoryBalances;
using Myrmex.Shared.Wms.Inventory;
using Myrmex.Tests.Wms.Inventory.Testing;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Inventory.Features.InventoryBalances;

public sealed class GetInventoryBalanceBySkuAndStorageLocationHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenExactBalanceExists_ReturnsCurrentDetailsAndVersion()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededInventoryBalance seeded = await InventoryBalanceTestData
            .SeedInventoryBalanceAsync(testDbContext.DbContext, quantity: 12.5m);
        GetInventoryBalanceBySkuAndStorageLocation.Handler handler =
            new(testDbContext.DbContext);

        ServiceResult<InventoryBalanceDetails> result = await handler.HandleAsync(
            new GetInventoryBalanceBySkuAndStorageLocation.Query(
                seeded.StockKeepingUnit.Id,
                seeded.StorageLocation.Id),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(seeded.InventoryBalance.Id, result.Value.Id);
        Assert.Equal(12.5m, result.Value.Quantity);
        Assert.Equal(
            Convert.ToBase64String(seeded.InventoryBalance.RowVersion),
            result.Value.BalanceVersion);
        Assert.Equal(seeded.StockKeepingUnit.Id, result.Value.Sku.Id);
        Assert.Equal(seeded.StorageLocation.Id, result.Value.StorageLocation.Id);
        Assert.Equal(seeded.Warehouse.Id, result.Value.StorageLocation.Warehouse.Id);
    }

    [Fact]
    public async Task HandleAsync_WhenRelatedReferencesAreInactive_StillReturnsCurrentDetails()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededInventoryBalance seeded = await InventoryBalanceTestData
            .SeedInventoryBalanceAsync(testDbContext.DbContext, quantity: 8);
        seeded.StockKeepingUnit.Deactivate();
        seeded.StorageLocation.Deactivate();
        seeded.StorageLocationType.Deactivate();
        seeded.StorageLocationStatus.Deactivate();
        await testDbContext.DbContext.SaveChangesAsync(
            TestContext.Current.CancellationToken);
        GetInventoryBalanceBySkuAndStorageLocation.Handler handler =
            new(testDbContext.DbContext);

        ServiceResult<InventoryBalanceDetails> result = await handler.HandleAsync(
            new GetInventoryBalanceBySkuAndStorageLocation.Query(
                seeded.StockKeepingUnit.Id,
                seeded.StorageLocation.Id),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(seeded.InventoryBalance.Id, result.Value.Id);
        Assert.Equal(8, result.Value.Quantity);
    }

    [Fact]
    public async Task HandleAsync_WhenExactPairIsMissing_ReturnsNotFound()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededInventoryReferences seeded = await InventoryBalanceTestData
            .SeedInventoryReferencesAsync(testDbContext.DbContext);
        GetInventoryBalanceBySkuAndStorageLocation.Handler handler =
            new(testDbContext.DbContext);

        ServiceResult<InventoryBalanceDetails> result = await handler.HandleAsync(
            new GetInventoryBalanceBySkuAndStorageLocation.Query(
                seeded.StockKeepingUnit.Id,
                seeded.StorageLocation.Id),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task HandleAsync_WhenCancellationIsRequested_PropagatesCancellation()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        GetInventoryBalanceBySkuAndStorageLocation.Handler handler =
            new(testDbContext.DbContext);
        using CancellationTokenSource cancellationTokenSource = new();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            handler.HandleAsync(
                new GetInventoryBalanceBySkuAndStorageLocation.Query(
                    Guid.NewGuid(),
                    Guid.NewGuid()),
                cancellationTokenSource.Token));
    }
}
