using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;

namespace Myrmex.Modules.Wms.Infrastructure.Persistence.Configurations;

internal sealed class StockKeepingUnitConfiguration : IEntityTypeConfiguration<StockKeepingUnit>
{
    public void Configure(EntityTypeBuilder<StockKeepingUnit> builder)
    {
        builder.ToTable(WmsDatabaseNames.StockKeepingUnitsTable);

        builder.HasKey(x => x.Id)
            .HasName(WmsDatabaseNames.StockKeepingUnitPrimaryKey);

        builder.Ignore(x => x.DomainEvents);

        builder.Property(x => x.Code)
            .HasMaxLength(StockKeepingUnit.MaxCodeLength)
            .IsRequired();

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName(WmsDatabaseNames.StockKeepingUnitCodeUniqueIndex);

        builder.Property(x => x.Name)
            .HasMaxLength(StockKeepingUnit.MaxNameLength)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(StockKeepingUnit.MaxDescriptionLength)
            .IsRequired(false);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired(false);
    }
}
