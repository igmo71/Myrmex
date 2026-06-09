using Microsoft.EntityFrameworkCore;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;
using Myrmex.Modules.Wms.Topology.Domain.Zones;

namespace Myrmex.Modules.Wms.Infrastructure.Persistence;

internal sealed class WmsDbContext(DbContextOptions<WmsDbContext> options)
    : DbContext(options)
{
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<StorageLocation> StorageLocations => Set<StorageLocation>();
    public DbSet<StorageLocationType> StorageLocationTypes => Set<StorageLocationType>();
    public DbSet<StorageLocationStatus> StorageLocationStatuses => Set<StorageLocationStatus>();
    public DbSet<StockKeepingUnit> StockKeepingUnits => Set<StockKeepingUnit>();
    public DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("wms");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WmsDbContext).Assembly);
    }
}
