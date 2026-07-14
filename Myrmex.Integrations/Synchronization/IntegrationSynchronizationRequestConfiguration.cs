using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Myrmex.Integrations.Synchronization;

internal sealed class IntegrationSynchronizationRequestConfiguration
    : IEntityTypeConfiguration<IntegrationSynchronizationRequest>
{
    public void Configure(EntityTypeBuilder<IntegrationSynchronizationRequest> builder)
    {
        builder.ToTable(
            IntegrationSynchronizationDatabaseNames.SynchronizationRequestsTable);

        builder.HasKey(x => x.Id)
            .HasName(
                IntegrationSynchronizationDatabaseNames
                    .SynchronizationRequestPrimaryKey);

        builder.Property(x => x.SourceSystem)
            .HasMaxLength(IntegrationSynchronizationRequest.SourceSystemMaxLength)
            .HasColumnType("nvarchar(32)")
            .IsRequired();

        builder.Property(x => x.SourceInstance)
            .HasMaxLength(IntegrationSynchronizationRequest.SourceInstanceMaxLength)
            .HasColumnType("nvarchar(128)")
            .IsRequired();

        builder.Property(x => x.EntityType)
            .HasMaxLength(IntegrationSynchronizationRequest.EntityTypeMaxLength)
            .HasColumnType("nvarchar(32)")
            .IsRequired();

        builder.Property(x => x.ExternalId)
            .HasMaxLength(IntegrationSynchronizationRequest.ExternalIdMaxLength)
            .HasColumnType("nvarchar(128)")
            .IsRequired();

        builder.Property(x => x.ExternalDataVersion)
            .HasMaxLength(IntegrationSynchronizationRequest.ExternalDataVersionMaxLength)
            .HasColumnType("varbinary(128)")
            .IsRequired();

        builder.Property(x => x.ExternalDocumentNumber)
            .HasMaxLength(IntegrationSynchronizationRequest.ExternalDocumentNumberMaxLength)
            .HasColumnType("nvarchar(64)")
            .IsRequired(false);

        builder.Property(x => x.ExternalDocumentDate)
            .HasColumnType("datetime2")
            .IsRequired(false);

        builder.Property(x => x.Trigger)
            .HasMaxLength(IntegrationSynchronizationRequest.TriggerMaxLength)
            .HasColumnType("nvarchar(32)")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(IntegrationSynchronizationRequest.StatusMaxLength)
            .HasColumnType("nvarchar(32)")
            .IsRequired();

        builder.Property(x => x.ReceivedAtUtc)
            .IsRequired();

        builder.Property(x => x.ProcessingStartedAtUtc)
            .IsRequired(false);

        builder.Property(x => x.CompletedAtUtc)
            .IsRequired(false);

        builder.Property(x => x.AttemptCount)
            .IsRequired();

        builder.Property(x => x.NextAttemptAtUtc)
            .IsRequired(false);

        builder.Property(x => x.LastError)
            .HasMaxLength(IntegrationSynchronizationRequest.LastErrorMaxLength)
            .HasColumnType("nvarchar(2048)")
            .IsRequired(false);

        builder.HasIndex(x => new
            {
                x.SourceSystem,
                x.SourceInstance,
                x.EntityType,
                x.ExternalId,
                x.ExternalDataVersion
            })
            .IsUnique()
            .HasDatabaseName(
                IntegrationSynchronizationDatabaseNames
                    .SynchronizationRequestIdempotencyUniqueIndex);
    }
}
