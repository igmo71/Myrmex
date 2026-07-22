using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Myrmex.Modules.Wms.Receiving.Domain.ReceivingOrders;

namespace Myrmex.Modules.Wms.Infrastructure.Persistence.Configurations;

internal sealed class ReceivingOrderLineConfiguration : IEntityTypeConfiguration<ReceivingOrderLine>
{
    public void Configure(EntityTypeBuilder<ReceivingOrderLine> builder)
    {
        builder.ToTable(WmsDatabaseNames.ReceivingOrderLinesTable);

        builder.HasKey(x => x.Id)
            .HasName(WmsDatabaseNames.ReceivingOrderLinePrimaryKey);

        builder.Ignore(x => x.RemainingQuantity);
        builder.Ignore(x => x.IsFullyReceived);

        builder.Property(x => x.ReceivingOrderId)
            .IsRequired();

        builder.Property(x => x.StockKeepingUnitId)
            .IsRequired();

        builder.Property(x => x.PlannedQuantity)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.ReceivedQuantity)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired(false);

        builder.HasOne(x => x.StockKeepingUnit)
            .WithMany()
            .HasForeignKey(x => x.StockKeepingUnitId)
            .HasConstraintName(WmsDatabaseNames.ReceivingOrderLineStockKeepingUnitForeignKey)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ReceivingOrderId, x.StockKeepingUnitId })
            .IsUnique()
            .HasDatabaseName(WmsDatabaseNames.ReceivingOrderLineReceivingOrderIdStockKeepingUnitIdUniqueIndex);

        builder.HasIndex(x => x.StockKeepingUnitId)
            .HasDatabaseName(WmsDatabaseNames.ReceivingOrderLineStockKeepingUnitIdIndex);
    }
}
