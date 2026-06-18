using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryTransactions;
using Myrmex.Modules.Wms.Inventory.Features.InventoryAdjustments;
using Myrmex.Shared.Wms.Inventory;
using Myrmex.Tests.Wms.Inventory.Testing;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Inventory.Features.InventoryAdjustments;

public sealed class AdjustInventoryBalanceHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenExistingBalanceHasMaterialAdjustment_UpdatesBalanceAndCreatesLedger()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();
        SeededInventoryBalance seeded = await InventoryBalanceTestData.SeedInventoryBalanceAsync(
            testDbContext.DbContext,
            quantity: 10);
        AdjustInventoryBalance.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);
        string expectedVersion = Convert.ToBase64String(seeded.InventoryBalance.RowVersion);

        ServiceResult<InventoryBalanceDetails> result = await handler.HandleAsync(
            new AdjustInventoryBalance.Command(
                seeded.StockKeepingUnit.Id,
                seeded.StorageLocation.Id,
                CountedQuantity: 14,
                Reason: "  Cycle count correction  ",
                ExpectedBalanceVersion: expectedVersion),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(14, result.Value.Quantity);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.BalanceVersion));

        InventoryTransaction transaction = await testDbContext.DbContext.InventoryTransactions
            .Include(x => x.Entries)
            .SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(InventoryTransactionType.Adjustment, transaction.TransactionType);
        Assert.Equal("Cycle count correction", transaction.Reason);

        InventoryLedgerEntry entry = Assert.Single(transaction.Entries);
        Assert.Equal(seeded.StockKeepingUnit.Id, entry.StockKeepingUnitId);
        Assert.Equal(seeded.StorageLocation.Id, entry.StorageLocationId);
        Assert.Equal(4, entry.QuantityDelta);
        Assert.Equal(10, entry.BalanceBefore);
        Assert.Equal(14, entry.BalanceAfter);
    }
}
