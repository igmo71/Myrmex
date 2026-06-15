using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;

namespace Myrmex.Modules.Wms.Infrastructure.Persistence.Configurations;

internal sealed class StorageLocationConfiguration : IEntityTypeConfiguration<StorageLocation>
{
    public void Configure(EntityTypeBuilder<StorageLocation> builder)
    {
        builder.ToTable(WmsDatabaseNames.StorageLocationsTable);

        builder.HasKey(x => x.Id)
            .HasName(WmsDatabaseNames.StorageLocationPrimaryKey);

        builder.Ignore(x => x.DomainEvents);

        builder.Property(x => x.WarehouseId)
            .IsRequired();

        builder.Property(x => x.ZoneId)
            .IsRequired();

        builder.Property(x => x.StorageLocationTypeId)
            .IsRequired();

        builder.Property(x => x.StorageLocationStatusId)
            .IsRequired();

        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .HasConstraintName(WmsDatabaseNames.StorageLocationWarehouseForeignKey)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Zone)
            .WithMany()
            .HasForeignKey(x => x.ZoneId)
            .HasConstraintName(WmsDatabaseNames.StorageLocationZoneForeignKey)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.StorageLocationType)
            .WithMany()
            .HasForeignKey(x => x.StorageLocationTypeId)
            .HasConstraintName(WmsDatabaseNames.StorageLocationTypeForeignKey)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.StorageLocationStatus)
            .WithMany()
            .HasForeignKey(x => x.StorageLocationStatusId)
            .HasConstraintName(WmsDatabaseNames.StorageLocationStatusForeignKey)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.WarehouseId)
            .HasDatabaseName(WmsDatabaseNames.StorageLocationWarehouseIdIndex);

        builder.HasIndex(x => x.ZoneId)
            .HasDatabaseName(WmsDatabaseNames.StorageLocationZoneIdIndex);

        builder.HasIndex(x => x.StorageLocationTypeId)
            .HasDatabaseName(WmsDatabaseNames.StorageLocationTypeIdIndex);

        builder.HasIndex(x => x.StorageLocationStatusId)
            .HasDatabaseName(WmsDatabaseNames.StorageLocationStatusIdIndex);

        builder.HasIndex(x => new { x.WarehouseId, x.Code })
            .IsUnique()
            .HasDatabaseName(WmsDatabaseNames.StorageLocationWarehouseIdCodeUniqueIndex);

        builder.Property(x => x.Code)
            .HasMaxLength(StorageLocation.MaxCodeLength)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(StorageLocation.MaxNameLength)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(StorageLocation.MaxDescriptionLength)
            .IsRequired(false);

        builder.Property(x => x.IsPickable)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired(false);
    }
}