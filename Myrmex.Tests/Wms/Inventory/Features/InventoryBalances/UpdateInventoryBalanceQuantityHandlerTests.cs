using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Inventory.Features.InventoryBalances;
using Myrmex.Shared.Wms.Inventory;
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
}
