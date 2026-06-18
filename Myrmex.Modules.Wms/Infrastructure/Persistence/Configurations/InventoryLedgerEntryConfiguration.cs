using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryTransactions;

namespace Myrmex.Modules.Wms.Infrastructure.Persistence.Configurations;

internal sealed class InventoryLedgerEntryConfiguration : IEntityTypeConfiguration<InventoryLedgerEntry>
{
    public void Configure(EntityTypeBuilder<InventoryLedgerEntry> builder)
    {
        builder.ToTable(WmsDatabaseNames.InventoryLedgerEntriesTable);

        builder.HasKey(x => x.Id)
            .HasName(WmsDatabaseNames.InventoryLedgerEntryPrimaryKey);

        builder.Property(x => x.InventoryTransactionId)
            .IsRequired();

        builder.Property(x => x.StockKeepingUnitId)
            .IsRequired();

        builder.Property(x => x.StorageLocationId)
            .IsRequired();

        builder.Property(x => x.QuantityDelta)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.BalanceBefore)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.BalanceAfter)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.HasOne(x => x.StockKeepingUnit)
            .WithMany()
            .HasForeignKey(x => x.StockKeepingUnitId)
            .HasConstraintName(WmsDatabaseNames.InventoryLedgerEntryStockKeepingUnitForeignKey)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.StorageLocation)
            .WithMany()
            .HasForeignKey(x => x.StorageLocationId)
            .HasConstraintName(WmsDatabaseNames.InventoryLedgerEntryStorageLocationForeignKey)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.StockKeepingUnitId)
            .HasDatabaseName(WmsDatabaseNames.InventoryLedgerEntryStockKeepingUnitIdIndex);

        builder.HasIndex(x => x.StorageLocationId)
            .HasDatabaseName(WmsDatabaseNames.InventoryLedgerEntryStorageLocationIdIndex);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired(false);
    }
}
