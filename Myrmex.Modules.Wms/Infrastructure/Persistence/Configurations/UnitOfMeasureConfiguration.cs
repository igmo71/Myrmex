using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Domain;

namespace Myrmex.Modules.Wms.Infrastructure.Persistence.Configurations;

internal sealed class UnitOfMeasureConfiguration : IEntityTypeConfiguration<UnitOfMeasure>
{
    public void Configure(EntityTypeBuilder<UnitOfMeasure> builder)
    {
        builder.ToTable(WmsDatabaseNames.UnitsOfMeasureTable);

        builder.HasKey(x => x.Id)
            .HasName(WmsDatabaseNames.UnitOfMeasurePrimaryKey);

        builder.Ignore(x => x.DomainEvents);

        builder.Property(x => x.Code)
            .HasMaxLength(UnitOfMeasure.MaxCodeLength)
            .IsRequired();

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName(WmsDatabaseNames.UnitOfMeasureCodeUniqueIndex);

        builder.Property(x => x.Name)
            .HasMaxLength(UnitOfMeasure.MaxNameLength)
            .IsRequired();

        builder.Property(x => x.Symbol)
            .HasMaxLength(UnitOfMeasure.MaxSymbolLength)
            .IsRequired(false);

        builder.Property(x => x.IsActive)
            .IsRequired();

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
                .HasDatabaseName(WmsDatabaseNames.UnitOfMeasureExternalRefKeyUniqueIndex);

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
