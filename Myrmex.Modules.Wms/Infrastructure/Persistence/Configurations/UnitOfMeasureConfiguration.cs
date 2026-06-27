using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;

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

        builder.Property(x => x.ExternalRefKey)
            .IsRequired(false);

        builder.HasIndex(x => x.ExternalRefKey)
            .IsUnique()
            .HasFilter("[ExternalRefKey] IS NOT NULL")
            .HasDatabaseName(WmsDatabaseNames.UnitOfMeasureExternalRefKeyUniqueIndex);

        builder.Property(x => x.LastImportedAtUtc)
            .IsRequired(false);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired(false);
    }
}
