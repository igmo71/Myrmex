using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryTransfers;

namespace Myrmex.Modules.Wms.Infrastructure.Persistence.Configurations;

internal sealed class InventoryTransferConfiguration : IEntityTypeConfiguration<InventoryTransfer>
{
    public void Configure(EntityTypeBuilder<InventoryTransfer> builder)
    {
        builder.ToTable(WmsDatabaseNames.InventoryTransfersTable);

        builder.HasKey(x => x.Id)
            .HasName(WmsDatabaseNames.InventoryTransferPrimaryKey);

        builder.Ignore(x => x.DomainEvents);
        builder.Ignore(x => x.UsesTransit);

        builder.Property(x => x.Code)
            .HasMaxLength(WmsDatabaseNames.InventoryTransferCodeMaxLength)
            .IsRequired();

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName(WmsDatabaseNames.InventoryTransferCodeUniqueIndex);

        builder.Property(x => x.SourceWarehouseId)
            .IsRequired();

        builder.Property(x => x.DestinationWarehouseId)
            .IsRequired();

        builder.Property(x => x.TransitStorageLocationId)
            .IsRequired(false);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(WmsDatabaseNames.InventoryTransferStatusMaxLength)
            .IsRequired();

        builder.HasOne(x => x.SourceWarehouse)
            .WithMany()
            .HasForeignKey(x => x.SourceWarehouseId)
            .HasConstraintName(WmsDatabaseNames.InventoryTransferSourceWarehouseForeignKey)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DestinationWarehouse)
            .WithMany()
            .HasForeignKey(x => x.DestinationWarehouseId)
            .HasConstraintName(WmsDatabaseNames.InventoryTransferDestinationWarehouseForeignKey)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.TransitStorageLocation)
            .WithMany()
            .HasForeignKey(x => x.TransitStorageLocationId)
            .HasConstraintName(WmsDatabaseNames.InventoryTransferTransitStorageLocationForeignKey)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Lines)
            .WithOne(x => x.InventoryTransfer)
            .HasForeignKey(x => x.InventoryTransferId)
            .HasConstraintName(WmsDatabaseNames.InventoryTransferLineInventoryTransferForeignKey)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(x => x.Lines)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Movements)
            .WithOne(x => x.InventoryTransfer)
            .HasForeignKey(x => x.InventoryTransferId)
            .HasConstraintName(WmsDatabaseNames.InventoryTransferMovementInventoryTransferForeignKey)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(x => x.Movements)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => x.SourceWarehouseId)
            .HasDatabaseName(WmsDatabaseNames.InventoryTransferSourceWarehouseIdIndex);

        builder.HasIndex(x => x.DestinationWarehouseId)
            .HasDatabaseName(WmsDatabaseNames.InventoryTransferDestinationWarehouseIdIndex);

        builder.HasIndex(x => x.TransitStorageLocationId)
            .HasDatabaseName(WmsDatabaseNames.InventoryTransferTransitStorageLocationIdIndex);

        builder.HasIndex(x => x.Status)
            .HasDatabaseName(WmsDatabaseNames.InventoryTransferStatusIndex);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired(false);
    }
}
