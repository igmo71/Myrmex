using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Myrmex.Integrations.Synchronization;

namespace Myrmex.Integrations.Persistence.Configurations;

internal sealed class SynchronizationRequestConfiguration
    : IEntityTypeConfiguration<SynchronizationRequest>
{
    public void Configure(EntityTypeBuilder<SynchronizationRequest> builder)
    {
        builder.ToTable(
            SynchronizationDatabaseNames.SynchronizationRequestsTable);

        builder.HasKey(x => x.Id)
            .HasName(
                SynchronizationDatabaseNames
                    .SynchronizationRequestPrimaryKey);

        builder.Property(x => x.SourceSystem)
            .HasMaxLength(SynchronizationRequest.SourceSystemMaxLength)
            .HasColumnType("nvarchar(32)")
            .IsRequired();

        builder.Property(x => x.SourceInstance)
            .HasMaxLength(SynchronizationRequest.SourceInstanceMaxLength)
            .HasColumnType("nvarchar(128)")
            .IsRequired();

        builder.Property(x => x.EntityType)
            .HasMaxLength(SynchronizationRequest.EntityTypeMaxLength)
            .HasColumnType("nvarchar(32)")
            .IsRequired();

        builder.Property(x => x.ExternalId)
            .HasMaxLength(SynchronizationRequest.ExternalIdMaxLength)
            .HasColumnType("nvarchar(128)")
            .IsRequired();

        builder.Property(x => x.ExternalDataVersion)
            .HasMaxLength(SynchronizationRequest.ExternalDataVersionMaxLength)
            .HasColumnType("varbinary(128)")
            .IsRequired();

        builder.Property(x => x.ExternalDocumentNumber)
            .HasMaxLength(SynchronizationRequest.ExternalDocumentNumberMaxLength)
            .HasColumnType("nvarchar(64)")
            .IsRequired(false);

        builder.Property(x => x.ExternalDocumentDate)
            .HasColumnType("datetime2")
            .IsRequired(false);

        builder.Property(x => x.Trigger)
            .HasMaxLength(SynchronizationRequest.TriggerMaxLength)
            .HasColumnType("nvarchar(32)")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(SynchronizationRequest.StatusMaxLength)
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
            .HasMaxLength(SynchronizationRequest.LastErrorMaxLength)
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
                SynchronizationDatabaseNames
                    .SynchronizationRequestIdempotencyUniqueIndex);
    }
}
