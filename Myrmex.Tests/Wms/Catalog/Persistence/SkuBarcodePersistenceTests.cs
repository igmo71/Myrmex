using Microsoft.EntityFrameworkCore;
using Myrmex.Modules.Wms.Catalog.Domain.SkuBarcodes;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Catalog.Persistence;

public sealed class SkuBarcodePersistenceTests
{
    [Fact]
    public async Task EnsureCreated_CreatesSkuBarcodeTableWithoutNormalizedValueColumn()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        // Act
        var entityType = testDbContext.DbContext.Model.FindEntityType(typeof(SkuBarcode));

        // Assert
        Assert.NotNull(entityType);
        Assert.Equal(WmsDatabaseNames.SkuBarcodesTable, entityType.GetTableName());
        Assert.Equal("wms", entityType.GetSchema());
        Assert.DoesNotContain(entityType.GetProperties(), property =>
            property.Name == "NormalizedValue");
    }

    [Fact]
    public async Task Model_HasRequiredFieldsAndNullableOptionalFields()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        // Act
        var entityType = testDbContext.DbContext.Model.FindEntityType(typeof(SkuBarcode));

        // Assert
        Assert.NotNull(entityType);

        Assert.False(entityType.FindProperty(nameof(SkuBarcode.StockKeepingUnitId))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(SkuBarcode.Value))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(SkuBarcode.Symbology))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(SkuBarcode.IsPrimary))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(SkuBarcode.IsActive))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(SkuBarcode.CreatedAtUtc))!.IsNullable);
        Assert.True(entityType.FindProperty(nameof(SkuBarcode.UpdatedAtUtc))!.IsNullable);
    }

    [Fact]
    public async Task Model_HasRequiredStockKeepingUnitRelationship()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        // Act
        var entityType = testDbContext.DbContext.Model.FindEntityType(typeof(SkuBarcode));

        // Assert
        Assert.NotNull(entityType);

        var foreignKey = Assert.Single(entityType.GetForeignKeys(), candidate =>
            candidate.Properties.Select(property => property.Name).SequenceEqual(
                [nameof(SkuBarcode.StockKeepingUnitId)]));

        Assert.True(foreignKey.IsRequired);
        Assert.Equal(typeof(StockKeepingUnit), foreignKey.PrincipalEntityType.ClrType);
        Assert.Equal(WmsDatabaseNames.SkuBarcodeStockKeepingUnitForeignKey, foreignKey.GetConstraintName());
    }

    [Fact]
    public async Task Model_HasUniqueCaseSensitiveValueIndexAndSkuIndex()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        // Act
        var entityType = testDbContext.DbContext.Model.FindEntityType(typeof(SkuBarcode));

        // Assert
        Assert.NotNull(entityType);

        var valueIndex = Assert.Single(entityType.GetIndexes(), candidate =>
            candidate.GetDatabaseName() == WmsDatabaseNames.SkuBarcodeValueUniqueIndex);

        Assert.True(valueIndex.IsUnique);
        Assert.Equal([nameof(SkuBarcode.Value)], valueIndex.Properties.Select(property => property.Name).ToArray());

        var skuIndex = Assert.Single(entityType.GetIndexes(), candidate =>
            candidate.GetDatabaseName() == WmsDatabaseNames.SkuBarcodeStockKeepingUnitIdIndex);

        Assert.False(skuIndex.IsUnique);
        Assert.Equal(
            [nameof(SkuBarcode.StockKeepingUnitId)],
            skuIndex.Properties.Select(property => property.Name).ToArray());
    }

    [Fact]
    public async Task Model_PersistsSymbologyAsString()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        // Act
        var entityType = testDbContext.DbContext.Model.FindEntityType(typeof(SkuBarcode));

        // Assert
        Assert.NotNull(entityType);

        var symbologyProperty = entityType.FindProperty(nameof(SkuBarcode.Symbology));

        Assert.NotNull(symbologyProperty);
        Assert.Equal(32, symbologyProperty.GetMaxLength());

        var converter = symbologyProperty.GetTypeMapping().Converter;

        Assert.NotNull(converter);
        Assert.Equal(typeof(string), converter.ProviderClrType);
    }

    [Fact]
    public async Task SaveChanges_WhenTrimmedValueAlreadyExists_ThrowsDbUpdateException()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        StockKeepingUnit stockKeepingUnit = await AddStockKeepingUnitAsync(testDbContext);

        SkuBarcode firstBarcode = CreateSkuBarcode(stockKeepingUnit.Id, "abc");
        SkuBarcode duplicateBarcode = CreateSkuBarcode(stockKeepingUnit.Id, "  abc  ");

        testDbContext.DbContext.SkuBarcodes.Add(firstBarcode);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        testDbContext.DbContext.SkuBarcodes.Add(duplicateBarcode);

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(() =>
            testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveChanges_WhenValuesDifferOnlyByCase_SavesBothBarcodes()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        StockKeepingUnit stockKeepingUnit = await AddStockKeepingUnitAsync(testDbContext);

        testDbContext.DbContext.SkuBarcodes.Add(CreateSkuBarcode(stockKeepingUnit.Id, "abc"));
        testDbContext.DbContext.SkuBarcodes.Add(CreateSkuBarcode(stockKeepingUnit.Id, "ABC"));

        // Act
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        string[] values = await testDbContext.DbContext.SkuBarcodes
            .OrderBy(x => x.Value)
            .Select(x => x.Value)
            .ToArrayAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["ABC", "abc"], values);
    }

    private static async Task<StockKeepingUnit> AddStockKeepingUnitAsync(TestWmsDbContext testDbContext)
    {
        UnitOfMeasure baseUnitOfMeasure = CreateUnitOfMeasure();

        testDbContext.DbContext.UnitsOfMeasure.Add(baseUnitOfMeasure);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = StockKeepingUnit.Create(
            code: "ITEM-001",
            name: "Widget",
            description: null,
            baseUnitOfMeasureId: baseUnitOfMeasure.Id,
            out StockKeepingUnit? stockKeepingUnit);

        Assert.True(result.IsValid);
        Assert.NotNull(stockKeepingUnit);

        testDbContext.DbContext.StockKeepingUnits.Add(stockKeepingUnit);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return stockKeepingUnit;
    }

    private static UnitOfMeasure CreateUnitOfMeasure()
    {
        var result = UnitOfMeasure.Create(
            code: "EA",
            name: "Each",
            symbol: "ea",
            out UnitOfMeasure? unitOfMeasure);

        Assert.True(result.IsValid);
        Assert.NotNull(unitOfMeasure);

        unitOfMeasure.ClearDomainEvents();

        return unitOfMeasure;
    }

    private static SkuBarcode CreateSkuBarcode(
        Guid stockKeepingUnitId,
        string value)
    {
        var result = SkuBarcode.Create(
            stockKeepingUnitId,
            value,
            BarcodeSymbology.Code128,
            isPrimary: false,
            out SkuBarcode? skuBarcode);

        Assert.True(result.IsValid);
        Assert.NotNull(skuBarcode);

        return skuBarcode;
    }
}
