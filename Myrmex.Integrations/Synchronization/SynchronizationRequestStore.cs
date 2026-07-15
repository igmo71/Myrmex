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

    public async Task<IReadOnlyList<SynchronizationRequest>> GetEligibleBatchAsync(
        int batchSize,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(batchSize),
                "Batch size must be positive.");
        }

        return await dbContext.SynchronizationRequests
            .AsNoTracking()
            .Where(request =>
                request.Status == SynchronizationStatus.Pending &&
                (request.NextAttemptAtUtc == null ||
                    request.NextAttemptAtUtc <= nowUtc))
            .OrderBy(request => request.ReceivedAtUtc)
            .ThenBy(request => request.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkDeferredAsync(
        Guid requestId,
        string lastError,
        CancellationToken cancellationToken)
    {
        SynchronizationRequest request =
            await LoadTrackedAsync(requestId, cancellationToken);
        request.Status = SynchronizationStatus.Deferred;
        request.NextAttemptAtUtc = null;
        request.LastError = BoundLastError(lastError);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<SynchronizationRequest> StartProcessingAsync(
        Guid requestId,
        DateTimeOffset startedAtUtc,
        CancellationToken cancellationToken)
    {
        SynchronizationRequest request =
            await LoadTrackedAsync(requestId, cancellationToken);
        request.Status = SynchronizationStatus.Processing;
        request.AttemptCount++;
        request.ProcessingStartedAtUtc = startedAtUtc;
        request.NextAttemptAtUtc = null;

        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.Entry(request).State = EntityState.Detached;

        return request;
    }

    public async Task MarkCompletedAsync(
        Guid requestId,
        DateTimeOffset completedAtUtc,
        CancellationToken cancellationToken)
    {
        SynchronizationRequest request =
            await LoadTrackedAsync(requestId, cancellationToken);
        request.Status = SynchronizationStatus.Completed;
        request.CompletedAtUtc = completedAtUtc;
        request.NextAttemptAtUtc = null;
        request.LastError = null;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkPendingRetryAsync(
        Guid requestId,
        DateTimeOffset nextAttemptAtUtc,
        string lastError,
        CancellationToken cancellationToken)
    {
        SynchronizationRequest request =
            await LoadTrackedAsync(requestId, cancellationToken);
        request.Status = SynchronizationStatus.Pending;
        request.NextAttemptAtUtc = nextAttemptAtUtc;
        request.LastError = BoundLastError(lastError);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(
        Guid requestId,
        string lastError,
        CancellationToken cancellationToken)
    {
        SynchronizationRequest request =
            await LoadTrackedAsync(requestId, cancellationToken);
        request.Status = SynchronizationStatus.Failed;
        request.NextAttemptAtUtc = null;
        request.LastError = BoundLastError(lastError);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<SynchronizationRequest> LoadTrackedAsync(
        Guid requestId,
        CancellationToken cancellationToken) =>
        await dbContext.SynchronizationRequests
            .SingleAsync(
                request => request.Id == requestId,
                cancellationToken);

    private static string BoundLastError(string lastError)
    {
        if (string.IsNullOrWhiteSpace(lastError))
        {
            return "Synchronization processing failed.";
        }

        return lastError.Length <= SynchronizationRequest.LastErrorMaxLength
            ? lastError
            : lastError[..SynchronizationRequest.LastErrorMaxLength];
    }
}
