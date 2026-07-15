using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;
using Myrmex.Integrations.OneC.Configuration;
using Myrmex.Integrations.OneC.Notifications;
using Myrmex.Integrations.Persistence;
using Myrmex.Integrations.Persistence.Configurations;
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
            .FindEntityType(typeof(SynchronizationRequest))!;
        IKey key = entityType.FindPrimaryKey()!;

        Assert.Equal(
            SynchronizationDatabaseNames.SynchronizationRequestsTable,
            entityType.GetTableName());
        Assert.Equal(
            SynchronizationDatabaseNames.Schema,
            entityType.GetSchema());
        Assert.Equal(
            SynchronizationDatabaseNames.SynchronizationRequestPrimaryKey,
            key.GetName());
    }

    [Fact]
    public void Model_HasBoundedIdempotencyUniqueIndex()
    {
        using IntegrationDbContext dbContext =
            IntegrationSynchronizationTestHost.CreateModelDbContext();

        IEntityType entityType = dbContext.Model
            .FindEntityType(typeof(SynchronizationRequest))!;

        IIndex index = Assert.Single(entityType.GetIndexes(), candidate =>
            candidate.GetDatabaseName() ==
            SynchronizationDatabaseNames
                .SynchronizationRequestIdempotencyUniqueIndex);

        Assert.True(index.IsUnique);
        Assert.Equal(
            [
                nameof(SynchronizationRequest.SourceSystem),
                nameof(SynchronizationRequest.SourceInstance),
                nameof(SynchronizationRequest.EntityType),
                nameof(SynchronizationRequest.ExternalId),
                nameof(SynchronizationRequest.ExternalDataVersion)
            ],
            index.Properties.Select(property => property.Name).ToArray());
        AssertColumn(
            entityType,
            nameof(SynchronizationRequest.SourceSystem),
            "nvarchar(32)",
            SynchronizationRequest.SourceSystemMaxLength);
        AssertColumn(
            entityType,
            nameof(SynchronizationRequest.SourceInstance),
            "nvarchar(128)",
            SynchronizationRequest.SourceInstanceMaxLength);
        AssertColumn(
            entityType,
            nameof(SynchronizationRequest.EntityType),
            "nvarchar(32)",
            SynchronizationRequest.EntityTypeMaxLength);
        AssertColumn(
            entityType,
            nameof(SynchronizationRequest.ExternalId),
            "nvarchar(128)",
            SynchronizationRequest.ExternalIdMaxLength);
        AssertColumn(
            entityType,
            nameof(SynchronizationRequest.ExternalDataVersion),
            "varbinary(128)",
            SynchronizationRequest.ExternalDataVersionMaxLength);
    }

    [Fact]
    public void Model_HasBoundedDiagnosticColumns()
    {
        using IntegrationDbContext dbContext =
            IntegrationSynchronizationTestHost.CreateModelDbContext();

        IEntityType entityType = dbContext.Model
            .FindEntityType(typeof(SynchronizationRequest))!;

        AssertColumn(
            entityType,
            nameof(SynchronizationRequest.ExternalDocumentNumber),
            "nvarchar(64)",
            SynchronizationRequest.ExternalDocumentNumberMaxLength);
        Assert.Equal(
            "datetime2",
            entityType
                .FindProperty(nameof(SynchronizationRequest.ExternalDocumentDate))!
                .GetColumnType());
        AssertColumn(
            entityType,
            nameof(SynchronizationRequest.LastError),
            "nvarchar(2048)",
            SynchronizationRequest.LastErrorMaxLength);
    }

    [Fact]
    public void RequestFactory_PreservesSourceLocalDocumentDateAsUnspecifiedDiagnosticData()
    {
        OneCChangeNotificationRequest notification = new()
        {
            RefKey = "80066011-d7c7-11ef-bac8-00155d01d112",
            DataVersion = Convert.ToBase64String([1, 2, 3]),
            Number = "UT-00001004",
            Date = "2025-01-21T10:15:36"
        };
        OneCChangeNotificationValidationResult validation =
            new OneCChangeNotificationValidator().Validate(notification);
        Assert.True(validation.Succeeded);

        SynchronizationRequestFactory factory = new(
            Options.Create(new OneCIntegrationApiKeyOptions
            {
                SourceSystem = OneCIntegrationApiKeyOptions.DefaultSourceSystem,
                SourceInstance = "main-infobase",
                ApiKey = "development-only-key"
            }),
            TimeProvider.System);

        SynchronizationRequest request = factory.Create(
            notification,
            validation,
            SynchronizationEntityTypes.ReceivingOrder);

        Assert.Equal(new DateTime(2025, 1, 21, 10, 15, 36), request.ExternalDocumentDate);
        Assert.Equal(DateTimeKind.Unspecified, request.ExternalDocumentDate!.Value.Kind);
        Assert.Equal("UT-00001004", request.ExternalDocumentNumber);
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
