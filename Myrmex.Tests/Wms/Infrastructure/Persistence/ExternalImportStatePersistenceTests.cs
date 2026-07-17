using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Domain;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;

namespace Myrmex.Tests.Wms.Infrastructure.Persistence;

public sealed class ExternalImportStatePersistenceTests
{
    [Theory]
    [InlineData(typeof(Warehouse), WmsDatabaseNames.WarehousesTable, WmsDatabaseNames.WarehouseExternalRefKeyUniqueIndex)]
    [InlineData(typeof(UnitOfMeasure), WmsDatabaseNames.UnitsOfMeasureTable, WmsDatabaseNames.UnitOfMeasureExternalRefKeyUniqueIndex)]
    [InlineData(typeof(StockKeepingUnit), WmsDatabaseNames.StockKeepingUnitsTable, WmsDatabaseNames.StockKeepingUnitExternalRefKeyUniqueIndex)]
    public void Model_PreservesExternalColumnsAndNonNullIdentityUniqueness(
        Type aggregateType,
        string tableName,
        string indexName)
    {
        DbContextOptions<WmsDbContext> options = new DbContextOptionsBuilder<WmsDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=MyrmexModelOnly;Trusted_Connection=True")
            .Options;
        using WmsDbContext dbContext = new(options);
        IEntityType aggregate = dbContext.Model.FindEntityType(aggregateType)!;
        IEntityType importState = aggregate.FindNavigation("ImportState")!.TargetEntityType;
        StoreObjectIdentifier table = StoreObjectIdentifier.Table(tableName, "wms");

        IProperty refKey = importState.FindProperty(nameof(ExternalImportState.RefKey))!;
        IProperty dataVersion = importState.FindProperty(nameof(ExternalImportState.DataVersion))!;
        IProperty importedAtUtc = importState.FindProperty(nameof(ExternalImportState.ImportedAtUtc))!;

        Assert.Equal("ExternalRefKey", refKey.GetColumnName(table));
        Assert.Equal("ExternalDataVersion", dataVersion.GetColumnName(table));
        Assert.Equal("LastImportedAtUtc", importedAtUtc.GetColumnName(table));
        Assert.True(dataVersion.IsNullable);
        Assert.Equal(ExternalImportState.MaxDataVersionLength, dataVersion.GetMaxLength());

        IIndex index = Assert.Single(importState.GetIndexes(), candidate =>
            candidate.GetDatabaseName() == indexName);
        Assert.True(index.IsUnique);
        Assert.Equal("[ExternalRefKey] IS NOT NULL", index.GetFilter());
        Assert.Equal([nameof(ExternalImportState.RefKey)], index.Properties.Select(property => property.Name).ToArray());
    }
}
