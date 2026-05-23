using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;

namespace Myrmex.Modules.Wms.Infrastructure.Persistence.Configurations;

internal sealed class StorageLocationStatusConfiguration : IEntityTypeConfiguration<StorageLocationStatus>
{
    public void Configure(EntityTypeBuilder<StorageLocationStatus> builder)
    {
        builder.ToTable(WmsDatabaseNames.StorageLocationStatusesTable);

        builder.HasKey(x => x.Id)
            .HasName(WmsDatabaseNames.StorageLocationStatusPrimaryKey);

        builder.Property(x => x.Code)
            .HasMaxLength(StorageLocationStatus.MaxCodeLength)
            .IsRequired();

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName(WmsDatabaseNames.StorageLocationStatusCodeUniqueIndex);

        builder.Property(x => x.Name)
            .HasMaxLength(StorageLocationStatus.MaxNameLength)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(StorageLocationStatus.MaxDescriptionLength)
            .IsRequired(false);

        builder.Property(x => x.IsSystem)
            .IsRequired();

        builder.Property(x => x.SortOrder)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired(false);

        builder.HasData(
            new
            {
                Id = WmsSeedIds.StorageLocationStatusAvailable,
                Code = "AVAILABLE",
                Name = "Available",
                Description = "Storage location is available for operations.",
                IsSystem = true,
                SortOrder = 10,
                CreatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                UpdatedAtUtc = (DateTimeOffset?)null,
                IsActive = true
            },
            new
            {
                Id = WmsSeedIds.StorageLocationStatusBlocked,
                Code = "BLOCKED",
                Name = "Blocked",
                Description = "Storage location is blocked for operations.",
                IsSystem = true,
                SortOrder = 20,
                CreatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                UpdatedAtUtc = (DateTimeOffset?)null,
                IsActive = true
            },
            new
            {
                Id = WmsSeedIds.StorageLocationStatusMaintenance,
                Code = "MAINTENANCE",
                Name = "Maintenance",
                Description = "Storage location is under maintenance.",
                IsSystem = true,
                SortOrder = 30,
                CreatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                UpdatedAtUtc = (DateTimeOffset?)null,
                IsActive = true
            },
            new
            {
                Id = WmsSeedIds.StorageLocationStatusInventoryCheck,
                Code = "INVENTORY_CHECK",
                Name = "Inventory check",
                Description = "Storage location is under inventory check.",
                IsSystem = true,
                SortOrder = 40,
                CreatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                UpdatedAtUtc = (DateTimeOffset?)null,
                IsActive = true
            });
    }
}