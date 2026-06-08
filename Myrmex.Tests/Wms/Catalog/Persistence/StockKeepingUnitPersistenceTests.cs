using Microsoft.EntityFrameworkCore;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Catalog.Persistence;

public sealed class StockKeepingUnitPersistenceTests
{
    [Fact]
    public async Task EnsureCreated_CreatesStockKeepingUnitTableWithoutNormalizedCodeColumn()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        // Act
        var entityType = testDbContext.DbContext.Model.FindEntityType(typeof(StockKeepingUnit));

        // Assert
        Assert.NotNull(entityType);
        Assert.Equal(WmsDatabaseNames.StockKeepingUnitsTable, entityType.GetTableName());
        Assert.Equal("wms", entityType.GetSchema());
        Assert.DoesNotContain(entityType.GetProperties(), property =>
            property.Name == "NormalizedCode");
    }

    [Fact]
    public async Task Model_HasUniqueStockKeepingUnitCodeIndex()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        // Act
        var entityType = testDbContext.DbContext.Model.FindEntityType(typeof(StockKeepingUnit));

        // Assert
        Assert.NotNull(entityType);

        var index = Assert.Single(entityType.GetIndexes(), candidate =>
            candidate.GetDatabaseName() == WmsDatabaseNames.StockKeepingUnitCodeUniqueIndex);

        Assert.True(index.IsUnique);
        Assert.Equal(["Code"], index.Properties.Select(property => property.Name).ToArray());
    }

    [Fact]
    public async Task SaveChanges_WhenNormalizedCodeAlreadyExists_ThrowsDbUpdateException()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        var firstResult = StockKeepingUnit.Create(
            code: "ITEM-001",
            name: "Widget",
            description: null,
            out StockKeepingUnit? firstStockKeepingUnit);

        Assert.True(firstResult.IsValid);
        Assert.NotNull(firstStockKeepingUnit);

        var duplicateResult = StockKeepingUnit.Create(
            code: " item-001 ",
            name: "Another Widget",
            description: null,
            out StockKeepingUnit? duplicateStockKeepingUnit);

        Assert.True(duplicateResult.IsValid);
        Assert.NotNull(duplicateStockKeepingUnit);

        testDbContext.DbContext.StockKeepingUnits.Add(firstStockKeepingUnit);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        testDbContext.DbContext.StockKeepingUnits.Add(duplicateStockKeepingUnit);

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(() =>
            testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken));
    }
}
