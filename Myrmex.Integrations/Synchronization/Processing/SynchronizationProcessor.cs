using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Myrmex.Integrations.Synchronization.Configuration;

namespace Myrmex.Integrations.Synchronization.Processing;

internal sealed class SynchronizationProcessor(
    SynchronizationRequestStore store,
    ISynchronizationHandlerResolver handlerResolver,
    SynchronizationRetryPolicy retryPolicy,
    IOptions<SynchronizationOptions> options,
    TimeProvider timeProvider,
    ILogger<SynchronizationProcessor> logger)
{
    public async Task<int> ProcessEligibleBatchAsync(
        CancellationToken cancellationToken)
    {
        SynchronizationOptions currentOptions = options.Value;
        IReadOnlyList<SynchronizationRequest> requests =
            await store.GetEligibleBatchAsync(
                currentOptions.BatchSize,
                timeProvider.GetUtcNow(),
                cancellationToken);

        foreach (SynchronizationRequest request in requests)
        {
            await ProcessAsync(
                request,
                currentOptions,
                cancellationToken);
        }

        return requests.Count;
    }

    public async Task<int> ProcessEligibleUntilDrainedAsync(
        CancellationToken cancellationToken)
    {
        int totalProcessed = 0;

        while (true)
        {
            int processed = await ProcessEligibleBatchAsync(cancellationToken);
            if (processed == 0)
            {
                return totalProcessed;
            }

            totalProcessed += processed;
        }
    }

    private async Task ProcessAsync(
        SynchronizationRequest request,
        SynchronizationOptions currentOptions,
        CancellationToken cancellationToken)
    {
        ISynchronizationHandler? handler =
            handlerResolver.Resolve(request.EntityType);
        if (handler is null)
        {
            logger.LogInformation(
                "Deferring synchronization request {SynchronizationRequestId} because no handler is registered for {EntityType}.",
                request.Id,
                request.EntityType);

            await store.MarkDeferredAsync(
                request.Id,
                $"No synchronization handler is registered for {request.EntityType}.",
                cancellationToken);
            return;
        }

        DateTimeOffset startedAtUtc = timeProvider.GetUtcNow();
        SynchronizationRequest processingRequest =
            await store.StartProcessingAsync(
                request.Id,
                startedAtUtc,
                cancellationToken);

        logger.LogInformation(
            "Started synchronization request {SynchronizationRequestId} attempt {AttemptCount}.",
            processingRequest.Id,
            processingRequest.AttemptCount);

        SynchronizationHandlerResult result;
        try
        {
            result = await InvokeHandlerAsync(
                handler,
                processingRequest,
                currentOptions,
                cancellationToken);
        }
        catch (OperationCanceledException) when (
            cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(
                "Synchronization processing stopped during request {SynchronizationRequestId}; durable state remains Processing.",
                processingRequest.Id);
            throw;
        }
        catch (TimeoutException)
        {
            logger.LogWarning(
                "Synchronization request {SynchronizationRequestId} attempt {AttemptCount} timed out.",
                processingRequest.Id,
                processingRequest.AttemptCount);

            await ApplyTransientFailureAsync(
                processingRequest,
                "Processing attempt timed out.",
                cancellationToken);
            return;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Synchronization handler failed for request {SynchronizationRequestId} attempt {AttemptCount}.",
                processingRequest.Id,
                processingRequest.AttemptCount);

            await ApplyTransientFailureAsync(
                processingRequest,
                "Synchronization handler failed.",
                cancellationToken);
            return;
        }

        await ApplyHandlerResultAsync(
            processingRequest,
            result,
            cancellationToken);
    }

    private async Task<SynchronizationHandlerResult> InvokeHandlerAsync(
        ISynchronizationHandler handler,
        SynchronizationRequest request,
        SynchronizationOptions currentOptions,
        CancellationToken cancellationToken)
    {
        using CancellationTokenSource attemptCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        Task<SynchronizationHandlerResult> handling =
            handler.HandleAsync(request, attemptCancellation.Token);

        try
        {
            return await handling.WaitAsync(
                TimeSpan.FromSeconds(
                    currentOptions.ProcessingAttemptTimeoutSeconds),
                timeProvider,
                cancellationToken);
        }
        catch (TimeoutException)
        {
            await attemptCancellation.CancelAsync();
            throw;
        }
    }

    private async Task ApplyHandlerResultAsync(
        SynchronizationRequest request,
        SynchronizationHandlerResult result,
        CancellationToken cancellationToken)
    {
        switch (result.Kind)
        {
            case SynchronizationHandlerResultKind.Completed:
                await store.MarkCompletedAsync(
                    request.Id,
                    timeProvider.GetUtcNow(),
                    cancellationToken);
                logger.LogInformation(
                    "Completed synchronization request {SynchronizationRequestId} attempt {AttemptCount}.",
                    request.Id,
                    request.AttemptCount);
                break;

            case SynchronizationHandlerResultKind.TransientFailure:
                await ApplyTransientFailureAsync(
                    request,
                    result.Error ?? "Transient synchronization failure.",
                    cancellationToken);
                break;

            case SynchronizationHandlerResultKind.PermanentFailure:
                await store.MarkFailedAsync(
                    request.Id,
                    result.Error ?? "Permanent synchronization failure.",
                    cancellationToken);
                logger.LogWarning(
                    "Marked synchronization request {SynchronizationRequestId} failed after permanent failure on attempt {AttemptCount}.",
                    request.Id,
                    request.AttemptCount);
                break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported synchronization handler result {result.Kind}.");
        }
    }

    private async Task ApplyTransientFailureAsync(
        SynchronizationRequest request,
        string error,
        CancellationToken cancellationToken)
    {
        SynchronizationRetryDecision retryDecision =
            retryPolicy.GetTransientFailureDecision(
                options.Value,
                request.AttemptCount,
                timeProvider.GetUtcNow());

        if (retryDecision.ShouldRetry)
        {
            await store.MarkPendingRetryAsync(
                request.Id,
                retryDecision.NextAttemptAtUtc!.Value,
                error,
                cancellationToken);
            logger.LogWarning(
                "Scheduled retry for synchronization request {SynchronizationRequestId} after attempt {AttemptCount} at {NextAttemptAtUtc}.",
                request.Id,
                request.AttemptCount,
                retryDecision.NextAttemptAtUtc);
            return;
        }

        await store.MarkFailedAsync(
            request.Id,
            error,
            cancellationToken);
        logger.LogWarning(
            "Marked synchronization request {SynchronizationRequestId} failed after transient failure on attempt {AttemptCount} with no retries remaining.",
            request.Id,
            request.AttemptCount);
    }
}
