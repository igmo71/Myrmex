using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;
using Myrmex.Modules.Wms.Topology.Domain.Zones;

namespace Myrmex.Modules.Wms.Infrastructure.Persistence.Configurations;

internal sealed class ZoneConfiguration : IEntityTypeConfiguration<Zone>
{
    public void Configure(EntityTypeBuilder<Zone> builder)
    {
        builder.ToTable(WmsDatabaseNames.ZonesTable);

        builder.HasKey(x => x.Id)
            .HasName(WmsDatabaseNames.ZonePrimaryKey);

        builder.Ignore(x => x.DomainEvents);

        builder.Property(x => x.WarehouseId)
            .IsRequired();

        builder.HasIndex(x => x.WarehouseId)
            .HasDatabaseName(WmsDatabaseNames.ZoneWarehouseIdIndex);

        builder.HasIndex(x => new { x.WarehouseId, x.Code })
            .IsUnique()
            .HasDatabaseName(WmsDatabaseNames.ZoneWarehouseIdCodeUniqueIndex);

        builder.HasOne<Warehouse>()
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .HasConstraintName(WmsDatabaseNames.ZoneWarehouseForeignKey)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Code)
            .HasMaxLength(Zone.MaxCodeLength)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(Zone.MaxNameLength)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(Zone.MaxDescriptionLength)
            .IsRequired(false);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired(false);
    }
}