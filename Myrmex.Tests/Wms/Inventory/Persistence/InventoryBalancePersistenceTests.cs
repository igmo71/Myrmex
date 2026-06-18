using Microsoft.EntityFrameworkCore;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Inventory.Persistence;

public sealed class InventoryBalancePersistenceTests
{
    [Fact]
    public async Task Model_HasInventoryBalanceTableWithoutWarehouseOrUnitOfMeasureColumns()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        var entityType = testDbContext.DbContext.Model.FindEntityType(typeof(InventoryBalance));

        Assert.NotNull(entityType);
        Assert.Equal(WmsDatabaseNames.InventoryBalancesTable, entityType.GetTableName());
        Assert.Equal("wms", entityType.GetSchema());
        Assert.DoesNotContain(entityType.GetProperties(), property => property.Name == "WarehouseId");
        Assert.DoesNotContain(entityType.GetProperties(), property => property.Name == "UnitOfMeasureId");
        Assert.DoesNotContain(entityType.GetProperties(), property => property.Name == "IsActive");
    }

    [Fact]
    public async Task Model_HasRequiredStockKeepingUnitAndStorageLocationRelationships()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        var entityType = testDbContext.DbContext.Model.FindEntityType(typeof(InventoryBalance));

        Assert.NotNull(entityType);

        var stockKeepingUnitProperty = Assert.Single(entityType.GetProperties(), property =>
            property.Name == nameof(InventoryBalance.StockKeepingUnitId));
        var storageLocationProperty = Assert.Single(entityType.GetProperties(), property =>
            property.Name == nameof(InventoryBalance.StorageLocationId));

        Assert.False(stockKeepingUnitProperty.IsNullable);
        Assert.False(storageLocationProperty.IsNullable);

        var stockKeepingUnitForeignKey = Assert.Single(entityType.GetForeignKeys(), foreignKey =>
            foreignKey.GetConstraintName() == WmsDatabaseNames.InventoryBalanceStockKeepingUnitForeignKey);
        var storageLocationForeignKey = Assert.Single(entityType.GetForeignKeys(), foreignKey =>
            foreignKey.GetConstraintName() == WmsDatabaseNames.InventoryBalanceStorageLocationForeignKey);

        Assert.Equal(typeof(StockKeepingUnit), stockKeepingUnitForeignKey.PrincipalEntityType.ClrType);
        Assert.Equal([nameof(InventoryBalance.StockKeepingUnitId)], stockKeepingUnitForeignKey.Properties.Select(property => property.Name).ToArray());
        Assert.Equal(DeleteBehavior.Restrict, stockKeepingUnitForeignKey.DeleteBehavior);

        Assert.Equal(typeof(StorageLocation), storageLocationForeignKey.PrincipalEntityType.ClrType);
        Assert.Equal([nameof(InventoryBalance.StorageLocationId)], storageLocationForeignKey.Properties.Select(property => property.Name).ToArray());
        Assert.Equal(DeleteBehavior.Restrict, storageLocationForeignKey.DeleteBehavior);
    }

    [Fact]
    public async Task Model_HasUniqueStockKeepingUnitStorageLocationIndexAndStorageLocationIndex()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        var entityType = testDbContext.DbContext.Model.FindEntityType(typeof(InventoryBalance));

        Assert.NotNull(entityType);

        var uniqueIndex = Assert.Single(entityType.GetIndexes(), index =>
            index.GetDatabaseName() == WmsDatabaseNames.InventoryBalanceStockKeepingUnitIdStorageLocationIdUniqueIndex);
        var storageLocationIndex = Assert.Single(entityType.GetIndexes(), index =>
            index.GetDatabaseName() == WmsDatabaseNames.InventoryBalanceStorageLocationIdIndex);

        Assert.True(uniqueIndex.IsUnique);
        Assert.Equal(
            [nameof(InventoryBalance.StockKeepingUnitId), nameof(InventoryBalance.StorageLocationId)],
            uniqueIndex.Properties.Select(property => property.Name).ToArray());

        Assert.False(storageLocationIndex.IsUnique);
        Assert.Equal([nameof(InventoryBalance.StorageLocationId)], storageLocationIndex.Properties.Select(property => property.Name).ToArray());
    }

    [Fact]
    public async Task Model_HasExplicitQuantityTimestampAndRowVersionMapping()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        var entityType = testDbContext.DbContext.Model.FindEntityType(typeof(InventoryBalance));

        Assert.NotNull(entityType);

        var quantity = Assert.Single(entityType.GetProperties(), property =>
            property.Name == nameof(InventoryBalance.Quantity));
        var createdAtUtc = Assert.Single(entityType.GetProperties(), property =>
            property.Name == nameof(InventoryBalance.CreatedAtUtc));
        var updatedAtUtc = Assert.Single(entityType.GetProperties(), property =>
            property.Name == nameof(InventoryBalance.UpdatedAtUtc));
        var rowVersion = Assert.Single(entityType.GetProperties(), property =>
            property.Name == nameof(InventoryBalance.RowVersion));

        Assert.Equal(18, quantity.GetPrecision());
        Assert.Equal(4, quantity.GetScale());
        Assert.False(quantity.IsNullable);
        Assert.False(createdAtUtc.IsNullable);
        Assert.True(updatedAtUtc.IsNullable);
        Assert.True(rowVersion.IsConcurrencyToken);
        Assert.Equal("row_version", rowVersion.GetColumnName());
    }
}
