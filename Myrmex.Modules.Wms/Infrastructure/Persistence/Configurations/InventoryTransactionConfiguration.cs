using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryTransactions;

namespace Myrmex.Modules.Wms.Infrastructure.Persistence.Configurations;

internal sealed class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
    {
        builder.ToTable(WmsDatabaseNames.InventoryTransactionsTable);

        builder.HasKey(x => x.Id)
            .HasName(WmsDatabaseNames.InventoryTransactionPrimaryKey);

        builder.Ignore(x => x.DomainEvents);

        builder.Property(x => x.TransactionType)
            .HasConversion<string>()
            .HasMaxLength(WmsDatabaseNames.InventoryTransactionTypeMaxLength)
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasMaxLength(WmsDatabaseNames.InventoryTransactionReasonMaxLength)
            .IsRequired();

        builder.Property(x => x.OccurredAtUtc)
            .IsRequired();

        builder.HasIndex(x => x.OccurredAtUtc)
            .HasDatabaseName(WmsDatabaseNames.InventoryTransactionOccurredAtUtcIndex);

        builder.HasMany(x => x.Entries)
            .WithOne(x => x.InventoryTransaction)
            .HasForeignKey(x => x.InventoryTransactionId)
            .HasConstraintName(WmsDatabaseNames.InventoryLedgerEntryInventoryTransactionForeignKey)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(x => x.Entries)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired(false);
    }
}
