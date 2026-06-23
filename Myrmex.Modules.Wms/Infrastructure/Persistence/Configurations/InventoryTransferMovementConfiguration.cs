using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryTransfers;

namespace Myrmex.Modules.Wms.Infrastructure.Persistence.Configurations;

internal sealed class InventoryTransferMovementConfiguration : IEntityTypeConfiguration<InventoryTransferMovement>
{
    public void Configure(EntityTypeBuilder<InventoryTransferMovement> builder)
    {
        builder.ToTable(WmsDatabaseNames.InventoryTransferMovementsTable);

        builder.HasKey(x => x.Id)
            .HasName(WmsDatabaseNames.InventoryTransferMovementPrimaryKey);

        builder.Property(x => x.InventoryTransferId)
            .IsRequired();

        builder.Property(x => x.InventoryTransferLineId)
            .IsRequired();

        builder.Property(x => x.InventoryTransactionId)
            .IsRequired();

        builder.Property(x => x.StockKeepingUnitId)
            .IsRequired();

        builder.Property(x => x.FromStorageLocationId)
            .IsRequired();

        builder.Property(x => x.ToStorageLocationId)
            .IsRequired();

        builder.Property(x => x.Quantity)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.OccurredAtUtc)
            .IsRequired();

        builder.HasOne(x => x.InventoryTransferLine)
            .WithMany()
            .HasForeignKey(x => x.InventoryTransferLineId)
            .HasConstraintName(WmsDatabaseNames.InventoryTransferMovementInventoryTransferLineForeignKey)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.InventoryTransaction)
            .WithMany()
            .HasForeignKey(x => x.InventoryTransactionId)
            .HasConstraintName(WmsDatabaseNames.InventoryTransferMovementInventoryTransactionForeignKey)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.StockKeepingUnit)
            .WithMany()
            .HasForeignKey(x => x.StockKeepingUnitId)
            .HasConstraintName(WmsDatabaseNames.InventoryTransferMovementStockKeepingUnitForeignKey)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.FromStorageLocation)
            .WithMany()
            .HasForeignKey(x => x.FromStorageLocationId)
            .HasConstraintName(WmsDatabaseNames.InventoryTransferMovementFromStorageLocationForeignKey)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ToStorageLocation)
            .WithMany()
            .HasForeignKey(x => x.ToStorageLocationId)
            .HasConstraintName(WmsDatabaseNames.InventoryTransferMovementToStorageLocationForeignKey)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.InventoryTransferId)
            .HasDatabaseName(WmsDatabaseNames.InventoryTransferMovementInventoryTransferIdIndex);

        builder.HasIndex(x => x.InventoryTransferLineId)
            .HasDatabaseName(WmsDatabaseNames.InventoryTransferMovementInventoryTransferLineIdIndex);

        builder.HasIndex(x => x.InventoryTransactionId)
            .HasDatabaseName(WmsDatabaseNames.InventoryTransferMovementInventoryTransactionIdIndex);

        builder.HasIndex(x => x.StockKeepingUnitId)
            .HasDatabaseName(WmsDatabaseNames.InventoryTransferMovementStockKeepingUnitIdIndex);

        builder.HasIndex(x => x.FromStorageLocationId)
            .HasDatabaseName(WmsDatabaseNames.InventoryTransferMovementFromStorageLocationIdIndex);

        builder.HasIndex(x => x.ToStorageLocationId)
            .HasDatabaseName(WmsDatabaseNames.InventoryTransferMovementToStorageLocationIdIndex);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired(false);
    }
}
