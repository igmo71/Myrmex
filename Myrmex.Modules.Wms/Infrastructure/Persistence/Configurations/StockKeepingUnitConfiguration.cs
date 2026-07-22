using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Domain;

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

        builder.Property(x => x.WeightKilograms)
            .HasPrecision(28, 12)
            .IsRequired(false);

        builder.Property(x => x.LengthMetres)
            .HasPrecision(28, 12)
            .IsRequired(false);

        builder.Property(x => x.AreaSquareMetres)
            .HasPrecision(28, 12)
            .IsRequired(false);

        builder.Property(x => x.VolumeCubicMetres)
            .HasPrecision(28, 12)
            .IsRequired(false);

        builder.Ignore(x => x.ExternalRefKey);
        builder.Ignore(x => x.ExternalDataVersion);
        builder.Ignore(x => x.LastImportedAtUtc);

        builder.OwnsOne(x => x.ImportState, importState =>
        {
            importState.Property(x => x.RefKey)
                .HasColumnName("ExternalRefKey")
                .IsRequired();

            importState.HasIndex(x => x.RefKey)
                .IsUnique()
                .HasFilter("[ExternalRefKey] IS NOT NULL")
                .HasDatabaseName(WmsDatabaseNames.StockKeepingUnitExternalRefKeyUniqueIndex);

            importState.Property(x => x.DataVersion)
                .HasField("_dataVersion")
                .UsePropertyAccessMode(PropertyAccessMode.Field)
                .HasColumnName("ExternalDataVersion")
                .HasMaxLength(ExternalImportState.MaxDataVersionLength)
                .IsRequired(false);

            importState.Property(x => x.ImportedAtUtc)
                .HasColumnName("LastImportedAtUtc")
                .IsRequired();
        });

        builder.Navigation(x => x.ImportState)
            .IsRequired(false);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired(false);
    }
}
