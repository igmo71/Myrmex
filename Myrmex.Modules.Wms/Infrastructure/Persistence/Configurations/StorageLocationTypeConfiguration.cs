using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;

namespace Myrmex.Modules.Wms.Infrastructure.Persistence.Configurations;

internal sealed class StorageLocationTypeConfiguration : IEntityTypeConfiguration<StorageLocationType>
{
    public void Configure(EntityTypeBuilder<StorageLocationType> builder)
    {
        builder.ToTable(WmsDatabaseNames.StorageLocationTypesTable);

        builder.HasKey(x => x.Id)
            .HasName(WmsDatabaseNames.StorageLocationTypePrimaryKey);

        builder.Property(x => x.Code)
            .HasMaxLength(StorageLocationType.MaxCodeLength)
            .IsRequired();

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName(WmsDatabaseNames.StorageLocationTypeCodeUniqueIndex);

        builder.Property(x => x.Name)
            .HasMaxLength(StorageLocationType.MaxNameLength)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(StorageLocationType.MaxDescriptionLength)
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
                Id = WmsSeedIds.StorageLocationTypePalletRack,
                Code = "PALLET_RACK",
                Name = "Pallet rack",
                Description = "Pallet rack storage location.",
                IsSystem = true,
                SortOrder = 10,
                CreatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                UpdatedAtUtc = (DateTimeOffset?)null,
                IsActive = true
            },
            new
            {
                Id = WmsSeedIds.StorageLocationTypeShelf,
                Code = "SHELF",
                Name = "Shelf",
                Description = "Shelf or bin storage location.",
                IsSystem = true,
                SortOrder = 20,
                CreatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                UpdatedAtUtc = (DateTimeOffset?)null,
                IsActive = true
            },
            new
            {
                Id = WmsSeedIds.StorageLocationTypeFloor,
                Code = "FLOOR",
                Name = "Floor",
                Description = "Floor storage location.",
                IsSystem = true,
                SortOrder = 30,
                CreatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                UpdatedAtUtc = (DateTimeOffset?)null,
                IsActive = true
            },
            new
            {
                Id = WmsSeedIds.StorageLocationTypeStaging,
                Code = "STAGING",
                Name = "Staging",
                Description = "Temporary staging location.",
                IsSystem = true,
                SortOrder = 40,
                CreatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                UpdatedAtUtc = (DateTimeOffset?)null,
                IsActive = true
            },
            new
            {
                Id = WmsSeedIds.StorageLocationTypeDock,
                Code = "DOCK",
                Name = "Dock",
                Description = "Receiving or shipping dock.",
                IsSystem = true,
                SortOrder = 50,
                CreatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                UpdatedAtUtc = (DateTimeOffset?)null,
                IsActive = true
            });
    }
}