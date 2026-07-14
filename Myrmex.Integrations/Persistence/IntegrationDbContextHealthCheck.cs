using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Myrmex.Integrations.Persistence;

internal sealed class IntegrationDbContextHealthCheck(
    IntegrationDbContext dbContext)
    : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        bool canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

        return canConnect
            ? HealthCheckResult.Healthy("Integration persistence is reachable.")
            : HealthCheckResult.Unhealthy("Integration persistence is unreachable.");
    }
}
