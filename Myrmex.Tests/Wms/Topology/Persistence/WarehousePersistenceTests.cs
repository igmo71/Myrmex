using Microsoft.EntityFrameworkCore;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Topology.Persistence;

public sealed class WarehousePersistenceTests
{
    [Fact]
    public async Task Model_HasNullableImportMetadataAndFilteredUniqueExternalRefKeyIndex()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        var entityType = testDbContext.DbContext.Model.FindEntityType(typeof(Warehouse));

        Assert.NotNull(entityType);
        Assert.True(entityType.FindProperty(nameof(Warehouse.ExternalRefKey))!.IsNullable);
        Assert.True(entityType.FindProperty(nameof(Warehouse.LastImportedAtUtc))!.IsNullable);

        var index = Assert.Single(entityType.GetIndexes(), candidate =>
            candidate.GetDatabaseName() == WmsDatabaseNames.WarehouseExternalRefKeyUniqueIndex);
        Assert.True(index.IsUnique);
        Assert.Equal("[ExternalRefKey] IS NOT NULL", index.GetFilter());
        Assert.Equal([nameof(Warehouse.ExternalRefKey)], index.Properties.Select(x => x.Name).ToArray());
    }
}
