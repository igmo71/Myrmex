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

        builder.Property(x => x.BaseUnitOfMeasureId)
            .IsRequired();

        builder.HasOne(x => x.BaseUnitOfMeasure)
            .WithMany()
            .HasForeignKey(x => x.BaseUnitOfMeasureId)
            .HasConstraintName(WmsDatabaseNames.StockKeepingUnitBaseUnitOfMeasureForeignKey)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.BaseUnitOfMeasureId)
            .HasDatabaseName(WmsDatabaseNames.StockKeepingUnitBaseUnitOfMeasureIdIndex);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.ExternalRefKey)
            .IsRequired(false);

        builder.HasIndex(x => x.ExternalRefKey)
            .IsUnique()
            .HasFilter("[ExternalRefKey] IS NOT NULL")
            .HasDatabaseName(WmsDatabaseNames.StockKeepingUnitExternalRefKeyUniqueIndex);

        builder.Property(x => x.LastImportedAtUtc)
            .IsRequired(false);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired(false);
    }
}
