using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryCounts;

namespace Myrmex.Modules.Wms.Infrastructure.Persistence.Configurations;

internal sealed class InventoryCountConfiguration : IEntityTypeConfiguration<InventoryCount>
{
    public void Configure(EntityTypeBuilder<InventoryCount> builder)
    {
        builder.ToTable(WmsDatabaseNames.InventoryCountsTable);

        builder.HasKey(x => x.Id)
            .HasName(WmsDatabaseNames.InventoryCountPrimaryKey);

        builder.Ignore(x => x.DomainEvents);

        builder.Property(x => x.WarehouseId).IsRequired();
        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(WmsDatabaseNames.InventoryCountStatusMaxLength)
            .IsRequired();
        builder.Property(x => x.Reason)
            .HasMaxLength(WmsDatabaseNames.InventoryCountReasonMaxLength)
            .IsRequired(false);
        builder.Property(x => x.CreatedByActorId)
            .HasMaxLength(WmsDatabaseNames.InventoryCountActorIdMaxLength)
            .IsRequired();
        builder.Property(x => x.CompletedByActorId)
            .HasMaxLength(WmsDatabaseNames.InventoryCountActorIdMaxLength)
            .IsRequired(false);
        builder.Property(x => x.CancelledByActorId)
            .HasMaxLength(WmsDatabaseNames.InventoryCountActorIdMaxLength)
            .IsRequired(false);
        builder.Property(x => x.CompletedAtUtc).IsRequired(false);
        builder.Property(x => x.CancelledAtUtc).IsRequired(false);
        builder.Property(x => x.RowVersion)
            .HasColumnName("row_version")
            .IsRowVersion();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired(false);

        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .HasConstraintName(WmsDatabaseNames.InventoryCountWarehouseForeignKey)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Lines)
            .WithOne(x => x.InventoryCount)
            .HasForeignKey(x => x.InventoryCountId)
            .HasConstraintName(WmsDatabaseNames.InventoryCountLineInventoryCountForeignKey)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(x => x.Lines)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => x.WarehouseId)
            .HasDatabaseName(WmsDatabaseNames.InventoryCountWarehouseIdIndex);
        builder.HasIndex(x => x.Status)
            .HasDatabaseName(WmsDatabaseNames.InventoryCountStatusIndex);
        builder.HasIndex(x => x.CreatedAtUtc)
            .HasDatabaseName(WmsDatabaseNames.InventoryCountCreatedAtUtcIndex);
    }
}
