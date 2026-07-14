using Microsoft.EntityFrameworkCore;
using Myrmex.Integrations.Synchronization;

namespace Myrmex.Tests.Integrations.OneC.Synchronization;

internal static class IntegrationSynchronizationTestHost
{
    public static IntegrationDbContext CreateModelDbContext()
    {
        DbContextOptions<IntegrationDbContext> options =
            new DbContextOptionsBuilder<IntegrationDbContext>()
                .UseSqlServer(
                    "Server=localhost;Database=MyrmexIntegrationModelTests;" +
                    "Trusted_Connection=True;TrustServerCertificate=True")
                .Options;

        return new IntegrationDbContext(options);
    }
}
