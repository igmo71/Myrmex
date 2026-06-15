using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Inventory.Features.InventoryBalances;
using Myrmex.Tests.Wms.Inventory.Testing;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Inventory.Features.InventoryBalances;

public sealed class GetInventoryBalanceByIdHandlerTests
{


    [Fact]
    public async Task HandleAsync_WhenInventoryBalanceHasZeroQuantity_ReturnsZeroQuantity()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededInventoryBalance seeded = await InventoryBalanceTestData.SeedInventoryBalanceAsync(
            testDbContext.DbContext,
            quantity: 0);

        GetInventoryBalanceById.Handler handler = new(testDbContext.DbContext);

        ServiceResult<InventoryBalanceDetails> result = await handler.HandleAsync(
            new GetInventoryBalanceById.Query(seeded.InventoryBalance.Id),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.Quantity);
    }

    [Fact]
    public async Task HandleAsync_WhenInventoryBalanceWasUpdated_ReturnsUpdatedTimestamp()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededInventoryBalance seeded = await InventoryBalanceTestData.SeedInventoryBalanceAsync(
            testDbContext.DbContext,
            quantity: 10);

        var updateResult = seeded.InventoryBalance.UpdateQuantity(5);
        Assert.True(updateResult.IsValid);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        GetInventoryBalanceById.Handler handler = new(testDbContext.DbContext);

        ServiceResult<InventoryBalanceDetails> result = await handler.HandleAsync(
            new GetInventoryBalanceById.Query(seeded.InventoryBalance.Id),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.Quantity);
        Assert.NotNull(result.Value.UpdatedAtUtc);
        Assert.Equal(seeded.InventoryBalance.UpdatedAtUtc, result.Value.UpdatedAtUtc);
    }

    [Fact]
    public async Task HandleAsync_WhenInventoryBalanceDoesNotExist_ReturnsNotFound()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        GetInventoryBalanceById.Handler handler = new(testDbContext.DbContext);

        ServiceResult<InventoryBalanceDetails> result = await handler.HandleAsync(
            new GetInventoryBalanceById.Query(Guid.NewGuid()),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal(ServiceErrorType.NotFound, result.Error.Type);
        Assert.Equal("InventoryBalance.NotFound", result.Error.Code);
        Assert.Equal("Inventory balance was not found.", result.Error.Message);
    }
}
