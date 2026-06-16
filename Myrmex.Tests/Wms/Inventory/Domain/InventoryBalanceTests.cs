using Myrmex.Core.Domain;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;

namespace Myrmex.Tests.Wms.Inventory.Domain;

public sealed class InventoryBalanceTests
{
    private static readonly Guid StockKeepingUnitId = Guid.Parse("018f0000-0000-7000-8000-000000000201");
    private static readonly Guid StorageLocationId = Guid.Parse("018f0000-0000-7000-8000-000000000301");

    [Fact]
    public void Create_WhenQuantityIsZero_CreatesInventoryBalance()
    {
        var result = InventoryBalance.Create(
            StockKeepingUnitId,
            StorageLocationId,
            quantity: 0,
            out InventoryBalance? inventoryBalance);

        Assert.True(result.IsValid);
        Assert.NotNull(inventoryBalance);

        Assert.Equal(StockKeepingUnitId, inventoryBalance.StockKeepingUnitId);
        Assert.Equal(StorageLocationId, inventoryBalance.StorageLocationId);
        Assert.Equal(0, inventoryBalance.Quantity);
        Assert.Null(inventoryBalance.UpdatedAtUtc);

        var domainEvent = Assert.Single(inventoryBalance.DomainEvents);
        InventoryBalanceCreatedDomainEvent createdEvent =
            Assert.IsType<InventoryBalanceCreatedDomainEvent>(domainEvent);

        Assert.Equal(inventoryBalance.Id, createdEvent.InventoryBalanceId);
        Assert.Equal(StockKeepingUnitId, createdEvent.StockKeepingUnitId);
        Assert.Equal(StorageLocationId, createdEvent.StorageLocationId);
        Assert.Equal(0, createdEvent.Quantity);
    }

    [Fact]
    public void InventoryBalance_DoesNotExposeActivationLifecycle()
    {
        Assert.False(typeof(IActivatable).IsAssignableFrom(typeof(InventoryBalance)));
        Assert.Null(typeof(InventoryBalance).GetProperty("IsActive"));
        Assert.DoesNotContain(typeof(InventoryBalance).GetMethods(), method =>
            method.Name is "Deactivate" or "Reactivate");
    }

    [Fact]
    public void UpdateQuantity_WhenQuantityIsValid_UpdatesOnlyQuantityAndTimestamp()
    {
        InventoryBalance inventoryBalance = CreateInventoryBalance(quantity: 10);
        DateTimeOffset createdAtUtc = inventoryBalance.CreatedAtUtc;
        inventoryBalance.ClearDomainEvents();

        var result = inventoryBalance.UpdateQuantity(5);

        Assert.True(result.IsValid);
        Assert.Equal(StockKeepingUnitId, inventoryBalance.StockKeepingUnitId);
        Assert.Equal(StorageLocationId, inventoryBalance.StorageLocationId);
        Assert.Equal(5, inventoryBalance.Quantity);
        Assert.Equal(createdAtUtc, inventoryBalance.CreatedAtUtc);
        Assert.NotNull(inventoryBalance.UpdatedAtUtc);

        var domainEvent = Assert.Single(inventoryBalance.DomainEvents);
        InventoryBalanceQuantityUpdatedDomainEvent updatedEvent =
            Assert.IsType<InventoryBalanceQuantityUpdatedDomainEvent>(domainEvent);

        Assert.Equal(inventoryBalance.Id, updatedEvent.InventoryBalanceId);
        Assert.Equal(5, updatedEvent.Quantity);
    }

    private static InventoryBalance CreateInventoryBalance(decimal quantity)
    {
        var result = InventoryBalance.Create(
            StockKeepingUnitId,
            StorageLocationId,
            quantity,
            out InventoryBalance? inventoryBalance);

        Assert.True(result.IsValid);
        Assert.NotNull(inventoryBalance);

        return inventoryBalance;
    }
}
