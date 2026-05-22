using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Myrmex.Modules.Wms.Capabilities.Topology.Domain.Warehouses;

namespace Myrmex.Modules.Wms.Infrastructure.Persistence.Configurations;

internal sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("warehouses");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .HasMaxLength(Warehouse.MaxCodeLength)
            .IsRequired();

        builder.HasIndex(x => x.Code)
            .IsUnique();

        builder.Property(x => x.Name)
            .HasMaxLength(Warehouse.MaxNameLength)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(Warehouse.MaxDescriptionLength);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc);
    }
}
