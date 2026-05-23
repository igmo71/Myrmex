using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;

namespace Myrmex.Modules.Wms.Infrastructure.Persistence.Configurations;

internal sealed class StorageLocationTypeConfiguration : IEntityTypeConfiguration<StorageLocationType>
{
    public void Configure(EntityTypeBuilder<StorageLocationType> builder)
    {
        builder.ToTable(WmsDatabaseNames.StorageLocationTypesTable);

        builder.HasKey(x => x.Id)
            .HasName(WmsDatabaseNames.StorageLocationTypePrimaryKey);

        builder.Property(x => x.Code)
            .HasMaxLength(StorageLocationType.MaxCodeLength)
            .IsRequired();

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName(WmsDatabaseNames.StorageLocationTypeCodeUniqueIndex);

        builder.Property(x => x.Name)
            .HasMaxLength(StorageLocationType.MaxNameLength)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(StorageLocationType.MaxDescriptionLength)
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