using Microsoft.EntityFrameworkCore;

namespace Myrmex.Integrations.Synchronization;

internal sealed class IntegrationDbContext(DbContextOptions<IntegrationDbContext> options)
    : DbContext(options)
{
    public DbSet<IntegrationSynchronizationRequest> SynchronizationRequests =>
        Set<IntegrationSynchronizationRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(IntegrationSynchronizationDatabaseNames.Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IntegrationDbContext).Assembly);
    }
}
