using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Myrmex.Modules.Wms.Domain;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;

namespace Myrmex.Modules.Wms.Infrastructure.Persistence.Configurations;

internal sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{

    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable(WmsDatabaseNames.WarehousesTable);

        builder.HasKey(x => x.Id)
            .HasName(WmsDatabaseNames.WarehousePrimaryKey);

        builder.Ignore(x => x.DomainEvents);

        builder.Property(x => x.Code)
            .HasMaxLength(Warehouse.MaxCodeLength)
            .IsRequired();

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName(WmsDatabaseNames.WarehouseCodeUniqueIndex);

        builder.Property(x => x.Name)
            .HasMaxLength(Warehouse.MaxNameLength)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(Warehouse.MaxDescriptionLength)
            .IsRequired(false);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.DefaultReceivingLocationId)
            .IsRequired(false);

        builder.HasOne(x => x.DefaultReceivingLocation)
            .WithMany()
            .HasForeignKey(x => x.DefaultReceivingLocationId)
            .HasConstraintName(WmsDatabaseNames.WarehouseDefaultReceivingLocationForeignKey)
            .OnDelete(DeleteBehavior.Restrict);

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
                .HasDatabaseName(WmsDatabaseNames.WarehouseExternalRefKeyUniqueIndex);

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
