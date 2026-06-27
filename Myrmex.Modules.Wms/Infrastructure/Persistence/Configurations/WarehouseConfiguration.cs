using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;

namespace Myrmex.Modules.Wms.Infrastructure.Persistence.Configurations;

internal sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{

    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable(WmsDatabaseNames.WarehousesTable);

        builder.HasKey(x => x.Id)
            .HasName(WmsDatabaseNames.WarehousePrimaryKey);

        builder.Ignore(x => x.DomainEvents);

        builder.Property(x => x.Code)
            .HasMaxLength(Warehouse.MaxCodeLength)
            .IsRequired();

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName(WmsDatabaseNames.WarehouseCodeUniqueIndex);

        builder.Property(x => x.Name)
            .HasMaxLength(Warehouse.MaxNameLength)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(Warehouse.MaxDescriptionLength)
            .IsRequired(false);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.ExternalRefKey)
            .IsRequired(false);

        builder.HasIndex(x => x.ExternalRefKey)
            .IsUnique()
            .HasFilter("[ExternalRefKey] IS NOT NULL")
            .HasDatabaseName(WmsDatabaseNames.WarehouseExternalRefKeyUniqueIndex);

        builder.Property(x => x.LastImportedAtUtc)
            .IsRequired(false);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired(false);
    }
}
