using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;

namespace Myrmex.Modules.Wms.Infrastructure.Persistence.Configurations;

internal sealed class StorageLocationStatusConfiguration : IEntityTypeConfiguration<StorageLocationStatus>
{
    public void Configure(EntityTypeBuilder<StorageLocationStatus> builder)
    {
        builder.ToTable(WmsDatabaseNames.StorageLocationStatusesTable);

        builder.HasKey(x => x.Id)
            .HasName(WmsDatabaseNames.StorageLocationStatusPrimaryKey);

        builder.Property(x => x.Code)
            .HasMaxLength(StorageLocationStatus.MaxCodeLength)
            .IsRequired();

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName(WmsDatabaseNames.StorageLocationStatusCodeUniqueIndex);

        builder.Property(x => x.Name)
            .HasMaxLength(StorageLocationStatus.MaxNameLength)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(StorageLocationStatus.MaxDescriptionLength)
            .IsRequired(false);

        builder.Property(x => x.IsSystem)
            .IsRequired();

        builder.Property(x => x.SortOrder)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired(false);
    }
}