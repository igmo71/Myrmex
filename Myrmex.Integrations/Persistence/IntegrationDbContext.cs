using Microsoft.EntityFrameworkCore;
using Myrmex.Integrations.Persistence.Configurations;
using Myrmex.Integrations.Synchronization;

namespace Myrmex.Integrations.Persistence;

internal sealed class IntegrationDbContext(DbContextOptions<IntegrationDbContext> options)
    : DbContext(options)
{
    public DbSet<SynchronizationRequest> SynchronizationRequests =>
        Set<SynchronizationRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SynchronizationDatabaseNames.Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IntegrationDbContext).Assembly);
    }
}
