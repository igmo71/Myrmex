using Myrmex.Integrations.Persistence;
using Myrmex.Integrations.Synchronization.Processing;

namespace Myrmex.Integrations.Synchronization;

internal sealed class SynchronizationRequestStore(
    IntegrationDbContext dbContext,
    SynchronizationWakeUp wakeUp)
{
    public async Task<SynchronizationRequest> InsertAsync(
        SynchronizationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        dbContext.SynchronizationRequests.Add(request);
        await dbContext.SaveChangesAsync(cancellationToken);

        wakeUp.Notify();

        return request;
    }
}
