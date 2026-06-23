using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryTransfers;

namespace Myrmex.Modules.Wms.Infrastructure.Persistence.Configurations;

internal sealed class InventoryTransferLineConfiguration : IEntityTypeConfiguration<InventoryTransferLine>
{
    public void Configure(EntityTypeBuilder<InventoryTransferLine> builder)
    {
        builder.ToTable(WmsDatabaseNames.InventoryTransferLinesTable);

        builder.HasKey(x => x.Id)
            .HasName(WmsDatabaseNames.InventoryTransferLinePrimaryKey);

        builder.Property(x => x.InventoryTransferId)
            .IsRequired();

        builder.Property(x => x.StockKeepingUnitId)
            .IsRequired();

        builder.Property(x => x.SourceStorageLocationId)
            .IsRequired();

        builder.Property(x => x.DestinationStorageLocationId)
            .IsRequired();

        builder.Property(x => x.RequestedQuantity)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.HasOne(x => x.StockKeepingUnit)
            .WithMany()
            .HasForeignKey(x => x.StockKeepingUnitId)
            .HasConstraintName(WmsDatabaseNames.InventoryTransferLineStockKeepingUnitForeignKey)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SourceStorageLocation)
            .WithMany()
            .HasForeignKey(x => x.SourceStorageLocationId)
            .HasConstraintName(WmsDatabaseNames.InventoryTransferLineSourceStorageLocationForeignKey)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DestinationStorageLocation)
            .WithMany()
            .HasForeignKey(x => x.DestinationStorageLocationId)
            .HasConstraintName(WmsDatabaseNames.InventoryTransferLineDestinationStorageLocationForeignKey)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.InventoryTransferId)
            .HasDatabaseName(WmsDatabaseNames.InventoryTransferLineInventoryTransferIdIndex);

        builder.HasIndex(x => x.StockKeepingUnitId)
            .HasDatabaseName(WmsDatabaseNames.InventoryTransferLineStockKeepingUnitIdIndex);

        builder.HasIndex(x => x.SourceStorageLocationId)
            .HasDatabaseName(WmsDatabaseNames.InventoryTransferLineSourceStorageLocationIdIndex);

        builder.HasIndex(x => x.DestinationStorageLocationId)
            .HasDatabaseName(WmsDatabaseNames.InventoryTransferLineDestinationStorageLocationIdIndex);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired(false);
    }
}
