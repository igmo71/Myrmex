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

        try
        {
            SynchronizationHandlerResult result =
                await InvokeHandlerAsync(
                    handler,
                    processingRequest,
                    currentOptions,
                    cancellationToken);

            await ApplyHandlerResultAsync(
                processingRequest,
                result,
                cancellationToken);
        }
        catch (OperationCanceledException) when (
            cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(
                "Synchronization processing stopped during request {SynchronizationRequestId}; durable state remains Processing.",
                processingRequest.Id);
        }
        catch (TimeoutException)
        {
            await ApplyTransientFailureAsync(
                processingRequest,
                "Processing attempt timed out.",
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Synchronization handler failed for request {SynchronizationRequestId}.",
                processingRequest.Id);

            await ApplyTransientFailureAsync(
                processingRequest,
                "Synchronization handler failed.",
                cancellationToken);
        }
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
            return;
        }

        await store.MarkFailedAsync(
            request.Id,
            error,
            cancellationToken);
    }
}
