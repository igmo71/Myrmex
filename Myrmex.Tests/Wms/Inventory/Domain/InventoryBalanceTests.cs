using Myrmex.Core.Domain;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;

namespace Myrmex.Tests.Wms.Inventory.Domain;

public sealed class InventoryBalanceTests
{
    private static readonly Guid StockKeepingUnitId = Guid.Parse("018f0000-0000-7000-8000-000000000201");
    private static readonly Guid StorageLocationId = Guid.Parse("018f0000-0000-7000-8000-000000000301");

    [Fact]
    public void Create_WhenStockKeepingUnitIdIsMissing_ReturnsValidationError()
    {
        var result = InventoryBalance.Create(
            stockKeepingUnitId: Guid.Empty,
            storageLocationId: StorageLocationId,
            quantity: 10,
            out InventoryBalance? inventoryBalance);

        Assert.False(result.IsValid);
        Assert.Null(inventoryBalance);

        var error = Assert.Single(result.Errors);

        Assert.Equal("InventoryBalance.StockKeepingUnitIdRequired", error.Code);
        Assert.Equal("stockKeepingUnitId", error.Property);
    }

    [Fact]
    public void Create_WhenStorageLocationIdIsMissing_ReturnsValidationError()
    {
        var result = InventoryBalance.Create(
            stockKeepingUnitId: StockKeepingUnitId,
            storageLocationId: Guid.Empty,
            quantity: 10,
            out InventoryBalance? inventoryBalance);

        Assert.False(result.IsValid);
        Assert.Null(inventoryBalance);

        var error = Assert.Single(result.Errors);

        Assert.Equal("InventoryBalance.StorageLocationIdRequired", error.Code);
        Assert.Equal("storageLocationId", error.Property);
    }

    [Fact]
    public void Create_WhenQuantityIsNegative_ReturnsValidationError()
    {
        var result = InventoryBalance.Create(
            StockKeepingUnitId,
            StorageLocationId,
            quantity: -1,
            out InventoryBalance? inventoryBalance);

        Assert.False(result.IsValid);
        Assert.Null(inventoryBalance);

        var error = Assert.Single(result.Errors);

        Assert.Equal("InventoryBalance.QuantityMustBeNonNegative", error.Code);
        Assert.Equal("quantity", error.Property);
    }

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
        Assert.Empty(typeof(InventoryBalance).GetMethods().Where(method =>
            method.Name is "Deactivate" or "Reactivate"));
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

    [Fact]
    public void UpdateQuantity_WhenQuantityIsNegative_ReturnsValidationError()
    {
        InventoryBalance inventoryBalance = CreateInventoryBalance(quantity: 10);

        var result = inventoryBalance.UpdateQuantity(-1);

        Assert.False(result.IsValid);
        Assert.Equal(10, inventoryBalance.Quantity);
        Assert.Null(inventoryBalance.UpdatedAtUtc);
        Assert.Equal("InventoryBalance.QuantityMustBeNonNegative", Assert.Single(result.Errors).Code);
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
