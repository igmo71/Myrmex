using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Myrmex.Modules.Wms.Catalog.Domain.SkuBarcodes;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;

namespace Myrmex.Modules.Wms.Infrastructure.Persistence.Configurations;

internal sealed class SkuBarcodeConfiguration : IEntityTypeConfiguration<SkuBarcode>
{
    public void Configure(EntityTypeBuilder<SkuBarcode> builder)
    {
        builder.ToTable(WmsDatabaseNames.SkuBarcodesTable);

        builder.HasKey(x => x.Id)
            .HasName(WmsDatabaseNames.SkuBarcodePrimaryKey);

        builder.Ignore(x => x.DomainEvents);

        builder.Property(x => x.StockKeepingUnitId)
            .IsRequired();

        builder.HasOne<StockKeepingUnit>()
            .WithMany()
            .HasForeignKey(x => x.StockKeepingUnitId)
            .HasConstraintName(WmsDatabaseNames.SkuBarcodeStockKeepingUnitForeignKey)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Value)
            .HasMaxLength(SkuBarcode.MaxValueLength)
            .IsRequired();

        builder.HasIndex(x => x.Value)
            .IsUnique()
            .HasDatabaseName(WmsDatabaseNames.SkuBarcodeValueUniqueIndex);

        builder.HasIndex(x => x.StockKeepingUnitId)
            .HasDatabaseName(WmsDatabaseNames.SkuBarcodeStockKeepingUnitIdIndex);

        builder.Property(x => x.Symbology)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.IsPrimary)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired(false);
    }
}
