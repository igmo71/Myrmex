using Myrmex.Integrations.Synchronization;
using Myrmex.Integrations.Synchronization.Processing;

namespace Myrmex.Integrations.OneC.References;

internal sealed class WarehouseReferenceSynchronizationHandler(
    IOneCReferenceSynchronizationService synchronizationService)
    : ISynchronizationHandler
{
    public string EntityType => SynchronizationEntityTypes.Warehouse;

    public Task<SynchronizationHandlerResult> HandleAsync(
        SynchronizationRequest request,
        CancellationToken cancellationToken) =>
        ReferenceSynchronizationHandlerMapping.HandleAsync(
            request,
            synchronizationService.SynchronizeWarehouseAsync,
            cancellationToken);
}

internal sealed class UnitOfMeasureReferenceSynchronizationHandler(
    IOneCReferenceSynchronizationService synchronizationService)
    : ISynchronizationHandler
{
    public string EntityType => SynchronizationEntityTypes.UnitOfMeasure;

    public Task<SynchronizationHandlerResult> HandleAsync(
        SynchronizationRequest request,
        CancellationToken cancellationToken) =>
        ReferenceSynchronizationHandlerMapping.HandleAsync(
            request,
            synchronizationService.SynchronizeUnitOfMeasureAsync,
            cancellationToken);
}

internal sealed class StockKeepingUnitReferenceSynchronizationHandler(
    IOneCReferenceSynchronizationService synchronizationService)
    : ISynchronizationHandler
{
    public string EntityType => SynchronizationEntityTypes.StockKeepingUnit;

    public Task<SynchronizationHandlerResult> HandleAsync(
        SynchronizationRequest request,
        CancellationToken cancellationToken) =>
        ReferenceSynchronizationHandlerMapping.HandleAsync(
            request,
            synchronizationService.SynchronizeStockKeepingUnitAsync,
            cancellationToken);
}

internal static class ReferenceSynchronizationHandlerMapping
{
    public static async Task<SynchronizationHandlerResult> HandleAsync(
        SynchronizationRequest request,
        Func<Guid, CancellationToken, Task<ReferenceSynchronizationResult>> synchronize,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.ExternalId, out Guid externalRefKey) || externalRefKey == Guid.Empty)
        {
            return SynchronizationHandlerResult.PermanentFailure(
                $"The synchronization request contains an invalid external identity: {request.ExternalId}.");
        }

        ReferenceSynchronizationResult result = await synchronize(externalRefKey, cancellationToken);
        return result.Outcome switch
        {
            ReferenceSynchronizationOutcome.Applied or
            ReferenceSynchronizationOutcome.Unchanged or
            ReferenceSynchronizationOutcome.ControlledSkip => SynchronizationHandlerResult.Completed(),

            ReferenceSynchronizationOutcome.Busy or
            ReferenceSynchronizationOutcome.TransientFailure =>
                SynchronizationHandlerResult.TransientFailure(result.Diagnostic),

            ReferenceSynchronizationOutcome.NotFound or
            ReferenceSynchronizationOutcome.PermanentFailure =>
                SynchronizationHandlerResult.PermanentFailure(result.Diagnostic),

            _ => throw new InvalidOperationException(
                $"Unsupported reference synchronization outcome {result.Outcome}.")
        };
    }
}
