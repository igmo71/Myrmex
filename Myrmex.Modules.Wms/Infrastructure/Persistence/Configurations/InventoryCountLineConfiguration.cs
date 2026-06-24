using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryCounts;

namespace Myrmex.Modules.Wms.Infrastructure.Persistence.Configurations;

internal sealed class InventoryCountLineConfiguration : IEntityTypeConfiguration<InventoryCountLine>
{
    public void Configure(EntityTypeBuilder<InventoryCountLine> builder)
    {
        builder.ToTable(WmsDatabaseNames.InventoryCountLinesTable);

        builder.HasKey(x => x.Id)
            .HasName(WmsDatabaseNames.InventoryCountLinePrimaryKey);

        builder.Property(x => x.InventoryCountId).IsRequired();
        builder.Property(x => x.StockKeepingUnitId).IsRequired();
        builder.Property(x => x.StorageLocationId).IsRequired();
        builder.Property(x => x.SystemQuantity).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.ExpectedBalanceVersion)
            .HasMaxLength(8)
            .IsRequired(false);
        builder.Property(x => x.CountedQuantity).HasPrecision(18, 4).IsRequired(false);
        builder.Property(x => x.VarianceQuantity).HasPrecision(18, 4).IsRequired(false);
        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(WmsDatabaseNames.InventoryCountLineStatusMaxLength)
            .IsRequired();
        builder.Property(x => x.IsCurrent).IsRequired();
        builder.Property(x => x.Comment)
            .HasMaxLength(WmsDatabaseNames.InventoryCountLineCommentMaxLength)
            .IsRequired(false);
        builder.Property(x => x.CountedByActorId)
            .HasMaxLength(WmsDatabaseNames.InventoryCountActorIdMaxLength)
            .IsRequired(false);
        builder.Property(x => x.CountedAtUtc).IsRequired(false);
        builder.Property(x => x.AppliedByActorId)
            .HasMaxLength(WmsDatabaseNames.InventoryCountActorIdMaxLength)
            .IsRequired(false);
        builder.Property(x => x.AppliedAtUtc).IsRequired(false);
        builder.Property(x => x.AppliedInventoryTransactionId).IsRequired(false);
        builder.Property(x => x.SupersedesInventoryCountLineId).IsRequired(false);
        builder.Property(x => x.RowVersion)
            .HasColumnName("row_version")
            .IsRowVersion();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired(false);

        builder.HasOne(x => x.StockKeepingUnit)
            .WithMany()
            .HasForeignKey(x => x.StockKeepingUnitId)
            .HasConstraintName(WmsDatabaseNames.InventoryCountLineStockKeepingUnitForeignKey)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.StorageLocation)
            .WithMany()
            .HasForeignKey(x => x.StorageLocationId)
            .HasConstraintName(WmsDatabaseNames.InventoryCountLineStorageLocationForeignKey)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AppliedInventoryTransaction)
            .WithMany()
            .HasForeignKey(x => x.AppliedInventoryTransactionId)
            .HasConstraintName(WmsDatabaseNames.InventoryCountLineAppliedInventoryTransactionForeignKey)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SupersedesInventoryCountLine)
            .WithOne(x => x.ReplacementInventoryCountLine)
            .HasForeignKey<InventoryCountLine>(x => x.SupersedesInventoryCountLineId)
            .HasConstraintName(WmsDatabaseNames.InventoryCountLineSupersedesForeignKey)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.InventoryCountId)
            .HasDatabaseName(WmsDatabaseNames.InventoryCountLineInventoryCountIdIndex);
        builder.HasIndex(x => x.StockKeepingUnitId)
            .HasDatabaseName(WmsDatabaseNames.InventoryCountLineStockKeepingUnitIdIndex);
        builder.HasIndex(x => x.StorageLocationId)
            .HasDatabaseName(WmsDatabaseNames.InventoryCountLineStorageLocationIdIndex);
        builder.HasIndex(x => x.Status)
            .HasDatabaseName(WmsDatabaseNames.InventoryCountLineStatusIndex);
        builder.HasIndex(x => new { x.InventoryCountId, x.StockKeepingUnitId, x.StorageLocationId })
            .IsUnique()
            .HasFilter("[IsCurrent] = CAST(1 AS bit)")
            .HasDatabaseName(WmsDatabaseNames.InventoryCountLineCurrentPairUniqueIndex);
        builder.HasIndex(x => x.AppliedInventoryTransactionId)
            .IsUnique()
            .HasFilter("[AppliedInventoryTransactionId] IS NOT NULL")
            .HasDatabaseName(WmsDatabaseNames.InventoryCountLineAppliedInventoryTransactionUniqueIndex);
        builder.HasIndex(x => x.SupersedesInventoryCountLineId)
            .IsUnique()
            .HasFilter("[SupersedesInventoryCountLineId] IS NOT NULL")
            .HasDatabaseName(WmsDatabaseNames.InventoryCountLineSupersedesUniqueIndex);
    }
}
