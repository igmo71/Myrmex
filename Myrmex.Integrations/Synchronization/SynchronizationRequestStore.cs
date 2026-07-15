using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Myrmex.Integrations.Persistence;
using Myrmex.Integrations.Persistence.SqlServer;
using Myrmex.Integrations.Synchronization.Processing;

namespace Myrmex.Integrations.Synchronization;

internal sealed class SynchronizationRequestStore(
    IntegrationDbContext dbContext,
    SynchronizationWakeUp wakeUp,
    SqlServerDuplicateSynchronizationRequestDetector duplicateDetector,
    ILogger<SynchronizationRequestStore> logger)
{
    public async Task<SynchronizationRequestIntakeResult> InsertAsync(
        SynchronizationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        dbContext.SynchronizationRequests.Add(request);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            duplicateDetector.IsIdempotencyDuplicate(exception))
        {
            dbContext.Entry(request).State = EntityState.Detached;

            SynchronizationRequest existing =
                await LoadExistingAsync(request, cancellationToken);

            logger.LogInformation(
                "Detected duplicate synchronization request for {SourceSystem}/{SourceInstance}/{EntityType}/{ExternalId}.",
                request.SourceSystem,
                request.SourceInstance,
                request.EntityType,
                request.ExternalId);

            if (existing.Status == SynchronizationStatus.Pending)
            {
                wakeUp.Notify();
            }

            return new SynchronizationRequestIntakeResult(
                existing,
                SynchronizationRequestIntakeResultKind.Duplicate);
        }

        wakeUp.Notify();

        return new SynchronizationRequestIntakeResult(
            request,
            SynchronizationRequestIntakeResultKind.Inserted);
    }

    private async Task<SynchronizationRequest> LoadExistingAsync(
        SynchronizationRequest request,
        CancellationToken cancellationToken)
    {
        SynchronizationRequest? existing =
            await dbContext.SynchronizationRequests
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    candidate =>
                        candidate.SourceSystem == request.SourceSystem &&
                        candidate.SourceInstance == request.SourceInstance &&
                        candidate.EntityType == request.EntityType &&
                        candidate.ExternalId == request.ExternalId &&
                        candidate.ExternalDataVersion.SequenceEqual(
                            request.ExternalDataVersion),
                    cancellationToken);

        return existing ??
            throw new InvalidOperationException(
                "The duplicate synchronization request could not be loaded.");
    }
}
