using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Myrmex.Modules.Wms.Domain;
using Myrmex.Modules.Wms.Receiving.Domain.ReceivingOrders;

namespace Myrmex.Modules.Wms.Infrastructure.Persistence.Configurations;

internal sealed class ReceivingOrderConfiguration : IEntityTypeConfiguration<ReceivingOrder>
{
    public void Configure(EntityTypeBuilder<ReceivingOrder> builder)
    {
        builder.ToTable(WmsDatabaseNames.ReceivingOrdersTable);

        builder.HasKey(x => x.Id)
            .HasName(WmsDatabaseNames.ReceivingOrderPrimaryKey);

        builder.Ignore(x => x.DomainEvents);
        builder.Ignore(x => x.IsFullyReceived);
        builder.Ignore(x => x.HasCompletePersistedCompletedInvariant);
        builder.Ignore(x => x.HasValidDraftPersistenceInvariant);
        builder.Ignore(x => x.ExternalRefKey);
        builder.Ignore(x => x.ExternalDataVersion);
        builder.Ignore(x => x.LastImportedAtUtc);

        builder.Property(x => x.Number)
            .HasMaxLength(ReceivingOrder.NumberMaxLength)
            .IsRequired();

        builder.Property(x => x.WarehouseId)
            .IsRequired();

        builder.Property(x => x.ReceivingLocationId)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(WmsDatabaseNames.ReceivingOrderStatusMaxLength)
            .IsRequired();

        builder.Property(x => x.StartedAtUtc)
            .IsRequired(false);

        builder.Property(x => x.CompletedAtUtc)
            .IsRequired(false);

        builder.Property(x => x.InventoryTransactionId)
            .IsRequired(false);

        builder.Property(x => x.RowVersion)
            .HasColumnName("row_version")
            .IsRowVersion();

        builder.OwnsOne(x => x.ImportState, importState =>
        {
            importState.Property(x => x.RefKey)
                .HasColumnName("ExternalRefKey")
                .IsRequired();
            importState.HasIndex(x => x.RefKey)
                .IsUnique()
                .HasFilter("[ExternalRefKey] IS NOT NULL")
                .HasDatabaseName(WmsDatabaseNames.ReceivingOrderExternalRefKeyUniqueIndex);
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
        builder.Navigation(x => x.ImportState).IsRequired(false);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired(false);

        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .HasConstraintName(WmsDatabaseNames.ReceivingOrderWarehouseForeignKey)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReceivingLocation)
            .WithMany()
            .HasForeignKey(x => x.ReceivingLocationId)
            .HasConstraintName(WmsDatabaseNames.ReceivingOrderReceivingLocationForeignKey)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.InventoryTransaction)
            .WithMany()
            .HasForeignKey(x => x.InventoryTransactionId)
            .HasConstraintName(WmsDatabaseNames.ReceivingOrderInventoryTransactionForeignKey)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Lines)
            .WithOne(x => x.ReceivingOrder)
            .HasForeignKey(x => x.ReceivingOrderId)
            .HasConstraintName(WmsDatabaseNames.ReceivingOrderLineReceivingOrderForeignKey)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Lines)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => x.Number)
            .IsUnique()
            .HasDatabaseName(WmsDatabaseNames.ReceivingOrderNumberUniqueIndex);

        builder.HasIndex(x => x.WarehouseId)
            .HasDatabaseName(WmsDatabaseNames.ReceivingOrderWarehouseIdIndex);

        builder.HasIndex(x => x.ReceivingLocationId)
            .HasDatabaseName(WmsDatabaseNames.ReceivingOrderReceivingLocationIdIndex);

        builder.HasIndex(x => x.InventoryTransactionId)
            .IsUnique()
            .HasFilter("[InventoryTransactionId] IS NOT NULL")
            .HasDatabaseName(WmsDatabaseNames.ReceivingOrderInventoryTransactionIdUniqueIndex);
    }
}
