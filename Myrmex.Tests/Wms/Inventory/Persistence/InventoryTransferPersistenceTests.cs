using Microsoft.EntityFrameworkCore;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryTransfers;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Inventory.Persistence;

public sealed class InventoryTransferPersistenceTests
{
    [Fact]
    public async Task Model_HasTransitStorageLocationTypeSeeds()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        StorageLocationType[] types = await testDbContext.DbContext.StorageLocationTypes
            .Where(x => x.Code == "INTERNAL_TRANSIT" || x.Code == "EXTERNAL_TRANSIT")
            .OrderBy(x => x.Code)
            .ToArrayAsync(TestContext.Current.CancellationToken);

        Assert.Collection(
            types,
            external =>
            {
                Assert.Equal("EXTERNAL_TRANSIT", external.Code);
                Assert.True(external.IsSystem);
                Assert.True(external.IsActive);
            },
            internalTransit =>
            {
                Assert.Equal("INTERNAL_TRANSIT", internalTransit.Code);
                Assert.True(internalTransit.IsSystem);
                Assert.True(internalTransit.IsActive);
            });
    }

    [Fact]
    public async Task Model_HasNullableTransitStorageLocationAndNoForbiddenPersistedFields()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        var entityType = testDbContext.DbContext.Model.FindEntityType(typeof(InventoryTransfer));

        Assert.NotNull(entityType);
        Assert.Equal(WmsDatabaseNames.InventoryTransfersTable, entityType.GetTableName());
        Assert.Equal("wms", entityType.GetSchema());

        var transitStorageLocationId = Assert.Single(entityType.GetProperties(), property =>
            property.Name == nameof(InventoryTransfer.TransitStorageLocationId));

        Assert.True(transitStorageLocationId.IsNullable);
        Assert.DoesNotContain(entityType.GetProperties(), property => property.Name == "TransferExecutionMode");
        Assert.DoesNotContain(entityType.GetProperties(), property => property.Name == "MovementType");
    }

    [Fact]
    public async Task Model_HasMovementWithoutPersistedStockKeepingUnit()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        var entityType = testDbContext.DbContext.Model.FindEntityType(typeof(InventoryTransferMovement));

        Assert.NotNull(entityType);
        Assert.DoesNotContain(entityType.GetProperties(), property =>
            property.Name == nameof(InventoryTransferLine.StockKeepingUnitId));
        Assert.DoesNotContain(entityType.GetForeignKeys(), foreignKey =>
            foreignKey.GetConstraintName() == "FK_wms_inventory_transfer_movements_stock_keeping_units_stock_keeping_unit_id");
        Assert.DoesNotContain(entityType.GetIndexes(), index =>
            index.GetDatabaseName() == "IX_wms_inventory_transfer_movements_stock_keeping_unit_id");
    }

    [Fact]
    public async Task Model_HasMovementInventoryTransactionRelationship()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        var entityType = testDbContext.DbContext.Model.FindEntityType(typeof(InventoryTransferMovement));

        Assert.NotNull(entityType);
        Assert.Equal(WmsDatabaseNames.InventoryTransferMovementsTable, entityType.GetTableName());

        var inventoryTransactionProperty = Assert.Single(entityType.GetProperties(), property =>
            property.Name == nameof(InventoryTransferMovement.InventoryTransactionId));
        var inventoryTransactionForeignKey = Assert.Single(entityType.GetForeignKeys(), foreignKey =>
            foreignKey.GetConstraintName() == WmsDatabaseNames.InventoryTransferMovementInventoryTransactionForeignKey);

        Assert.False(inventoryTransactionProperty.IsNullable);
        Assert.Equal(DeleteBehavior.Restrict, inventoryTransactionForeignKey.DeleteBehavior);
    }
}
