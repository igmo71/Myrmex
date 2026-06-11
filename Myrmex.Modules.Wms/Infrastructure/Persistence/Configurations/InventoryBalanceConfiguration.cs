using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;

namespace Myrmex.Modules.Wms.Infrastructure.Persistence.Configurations;

internal sealed class InventoryBalanceConfiguration : IEntityTypeConfiguration<InventoryBalance>
{
    public void Configure(EntityTypeBuilder<InventoryBalance> builder)
    {
        builder.ToTable(WmsDatabaseNames.InventoryBalancesTable);

        builder.HasKey(x => x.Id)
            .HasName(WmsDatabaseNames.InventoryBalancePrimaryKey);

        builder.Ignore(x => x.DomainEvents);

        builder.Property(x => x.StockKeepingUnitId)
            .IsRequired();

        builder.Property(x => x.StorageLocationId)
            .IsRequired();

        builder.HasOne<StockKeepingUnit>()
            .WithMany()
            .HasForeignKey(x => x.StockKeepingUnitId)
            .HasConstraintName(WmsDatabaseNames.InventoryBalanceStockKeepingUnitForeignKey)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<StorageLocation>()
            .WithMany()
            .HasForeignKey(x => x.StorageLocationId)
            .HasConstraintName(WmsDatabaseNames.InventoryBalanceStorageLocationForeignKey)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.StockKeepingUnitId, x.StorageLocationId })
            .IsUnique()
            .HasDatabaseName(WmsDatabaseNames.InventoryBalanceStockKeepingUnitIdStorageLocationIdUniqueIndex);

        builder.HasIndex(x => x.StorageLocationId)
            .HasDatabaseName(WmsDatabaseNames.InventoryBalanceStorageLocationIdIndex);

        builder.Property(x => x.Quantity)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired(false);
    }
}
