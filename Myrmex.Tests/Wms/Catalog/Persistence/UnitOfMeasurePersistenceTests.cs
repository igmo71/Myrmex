using Microsoft.EntityFrameworkCore;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Catalog.Persistence;

public sealed class UnitOfMeasurePersistenceTests
{
    [Fact]
    public async Task Model_HasRequiredFieldsAndNullableOptionalFields()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        // Act
        var entityType = testDbContext.DbContext.Model.FindEntityType(typeof(UnitOfMeasure));

        // Assert
        Assert.NotNull(entityType);
        Assert.Equal(WmsDatabaseNames.UnitsOfMeasureTable, entityType.GetTableName());
        Assert.Equal("wms", entityType.GetSchema());

        Assert.False(entityType.FindProperty(nameof(UnitOfMeasure.Code))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(UnitOfMeasure.Name))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(UnitOfMeasure.CreatedAtUtc))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(UnitOfMeasure.IsActive))!.IsNullable);
        Assert.True(entityType.FindProperty(nameof(UnitOfMeasure.Symbol))!.IsNullable);
        Assert.True(entityType.FindProperty(nameof(UnitOfMeasure.UpdatedAtUtc))!.IsNullable);
    }

    [Fact]
    public async Task Model_HasUniqueUnitOfMeasureCodeIndex()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        // Act
        var entityType = testDbContext.DbContext.Model.FindEntityType(typeof(UnitOfMeasure));

        // Assert
        Assert.NotNull(entityType);

        var index = Assert.Single(entityType.GetIndexes(), candidate =>
            candidate.GetDatabaseName() == WmsDatabaseNames.UnitOfMeasureCodeUniqueIndex);

        Assert.True(index.IsUnique);
        Assert.Equal(["Code"], index.Properties.Select(property => property.Name).ToArray());
    }

    [Fact]
    public async Task SaveChanges_WhenNormalizedCodeAlreadyExists_ThrowsDbUpdateException()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();

        var firstResult = UnitOfMeasure.Create(
            code: "EA",
            name: "Each",
            symbol: null,
            out UnitOfMeasure? firstUnitOfMeasure);

        Assert.True(firstResult.IsValid);
        Assert.NotNull(firstUnitOfMeasure);

        var duplicateResult = UnitOfMeasure.Create(
            code: " ea ",
            name: "Each duplicate",
            symbol: null,
            out UnitOfMeasure? duplicateUnitOfMeasure);

        Assert.True(duplicateResult.IsValid);
        Assert.NotNull(duplicateUnitOfMeasure);

        testDbContext.DbContext.UnitsOfMeasure.Add(firstUnitOfMeasure);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        testDbContext.DbContext.UnitsOfMeasure.Add(duplicateUnitOfMeasure);

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(() =>
            testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken));
    }
}
