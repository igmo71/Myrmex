using Microsoft.EntityFrameworkCore;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryCounts;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Inventory.Persistence;

public sealed class InventoryCountPersistenceTests
{
    [Fact]
    public async Task Model_ConfiguresCountAndLineAuditConcurrencyAndIndexes()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        var countType = testDbContext.DbContext.Model.FindEntityType(typeof(InventoryCount));
        var lineType = testDbContext.DbContext.Model.FindEntityType(typeof(InventoryCountLine));

        Assert.NotNull(countType);
        Assert.NotNull(lineType);
        Assert.Equal(WmsDatabaseNames.InventoryCountsTable, countType.GetTableName());
        Assert.Equal(WmsDatabaseNames.InventoryCountLinesTable, lineType.GetTableName());
        Assert.True(countType.FindProperty(nameof(InventoryCount.RowVersion))!.IsConcurrencyToken);
        Assert.True(lineType.FindProperty(nameof(InventoryCountLine.RowVersion))!.IsConcurrencyToken);
        Assert.Equal(500, countType.FindProperty(nameof(InventoryCount.Reason))!.GetMaxLength());
        Assert.Equal(256, countType.FindProperty(nameof(InventoryCount.CreatedByActorId))!.GetMaxLength());
        Assert.Equal(18, lineType.FindProperty(nameof(InventoryCountLine.SystemQuantity))!.GetPrecision());
        Assert.Equal(4, lineType.FindProperty(nameof(InventoryCountLine.SystemQuantity))!.GetScale());

        var currentPairIndex = Assert.Single(lineType.GetIndexes(), index =>
            index.GetDatabaseName() == WmsDatabaseNames.InventoryCountLineCurrentPairUniqueIndex);
        Assert.True(currentPairIndex.IsUnique);
        Assert.Contains("IsCurrent", currentPairIndex.GetFilter());

        Assert.Contains(lineType.GetIndexes(), index =>
            index.GetDatabaseName() == WmsDatabaseNames.InventoryCountLineSupersedesUniqueIndex &&
            index.IsUnique);
        Assert.Contains(lineType.GetIndexes(), index =>
            index.GetDatabaseName() == WmsDatabaseNames.InventoryCountLineAppliedInventoryTransactionUniqueIndex &&
            index.IsUnique);
    }
}
