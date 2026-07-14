using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Myrmex.Integrations.Synchronization;

namespace Myrmex.Tests.Integrations.OneC.Synchronization;

public sealed class IntegrationSynchronizationPersistenceTests
{
    [Fact]
    public void Model_UsesIntegrationSchemaAndSynchronizationRequestTable()
    {
        using IntegrationDbContext dbContext =
            IntegrationSynchronizationTestHost.CreateModelDbContext();

        IEntityType entityType = dbContext.Model
            .FindEntityType(typeof(IntegrationSynchronizationRequest))!;
        IKey key = entityType.FindPrimaryKey()!;

        Assert.Equal(
            IntegrationSynchronizationDatabaseNames.SynchronizationRequestsTable,
            entityType.GetTableName());
        Assert.Equal(
            IntegrationSynchronizationDatabaseNames.Schema,
            entityType.GetSchema());
        Assert.Equal(
            IntegrationSynchronizationDatabaseNames.SynchronizationRequestPrimaryKey,
            key.GetName());
    }

    [Fact]
    public void Model_HasBoundedIdempotencyUniqueIndex()
    {
        using IntegrationDbContext dbContext =
            IntegrationSynchronizationTestHost.CreateModelDbContext();

        IEntityType entityType = dbContext.Model
            .FindEntityType(typeof(IntegrationSynchronizationRequest))!;

        IIndex index = Assert.Single(entityType.GetIndexes(), candidate =>
            candidate.GetDatabaseName() ==
            IntegrationSynchronizationDatabaseNames
                .SynchronizationRequestIdempotencyUniqueIndex);

        Assert.True(index.IsUnique);
        Assert.Equal(
            [
                nameof(IntegrationSynchronizationRequest.SourceSystem),
                nameof(IntegrationSynchronizationRequest.SourceInstance),
                nameof(IntegrationSynchronizationRequest.EntityType),
                nameof(IntegrationSynchronizationRequest.ExternalId),
                nameof(IntegrationSynchronizationRequest.ExternalDataVersion)
            ],
            index.Properties.Select(property => property.Name).ToArray());
        AssertColumn(
            entityType,
            nameof(IntegrationSynchronizationRequest.SourceSystem),
            "nvarchar(32)",
            IntegrationSynchronizationRequest.SourceSystemMaxLength);
        AssertColumn(
            entityType,
            nameof(IntegrationSynchronizationRequest.SourceInstance),
            "nvarchar(128)",
            IntegrationSynchronizationRequest.SourceInstanceMaxLength);
        AssertColumn(
            entityType,
            nameof(IntegrationSynchronizationRequest.EntityType),
            "nvarchar(32)",
            IntegrationSynchronizationRequest.EntityTypeMaxLength);
        AssertColumn(
            entityType,
            nameof(IntegrationSynchronizationRequest.ExternalId),
            "nvarchar(128)",
            IntegrationSynchronizationRequest.ExternalIdMaxLength);
        AssertColumn(
            entityType,
            nameof(IntegrationSynchronizationRequest.ExternalDataVersion),
            "varbinary(128)",
            IntegrationSynchronizationRequest.ExternalDataVersionMaxLength);
    }

    [Fact]
    public void Model_HasBoundedDiagnosticColumns()
    {
        using IntegrationDbContext dbContext =
            IntegrationSynchronizationTestHost.CreateModelDbContext();

        IEntityType entityType = dbContext.Model
            .FindEntityType(typeof(IntegrationSynchronizationRequest))!;

        AssertColumn(
            entityType,
            nameof(IntegrationSynchronizationRequest.ExternalDocumentNumber),
            "nvarchar(64)",
            IntegrationSynchronizationRequest.ExternalDocumentNumberMaxLength);
        Assert.Equal(
            "datetime2",
            entityType
                .FindProperty(nameof(IntegrationSynchronizationRequest.ExternalDocumentDate))!
                .GetColumnType());
        AssertColumn(
            entityType,
            nameof(IntegrationSynchronizationRequest.LastError),
            "nvarchar(2048)",
            IntegrationSynchronizationRequest.LastErrorMaxLength);
    }

    private static void AssertColumn(
        IEntityType entityType,
        string propertyName,
        string columnType,
        int maximumLength)
    {
        IProperty property = entityType.FindProperty(propertyName)!;

        Assert.Equal(columnType, property.GetColumnType());
        Assert.Equal(maximumLength, property.GetMaxLength());
    }
}
