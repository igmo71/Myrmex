using Microsoft.Extensions.Logging;
using Myrmex.Integrations.OneC.Common.References;
using Myrmex.Integrations.Synchronization;
using Myrmex.Integrations.Synchronization.Processing;

namespace Myrmex.Integrations.OneC.StockKeepingUnits;

internal sealed class StockKeepingUnitReferenceSynchronizationHandler(
    IStockKeepingUnitOneCSynchronizer synchronizer,
    ReferenceSynchronizationHandlerResultMapper mapper,
    ILogger<StockKeepingUnitReferenceSynchronizationHandler> logger)
    : ISynchronizationHandler
{
    public string EntityType => SynchronizationEntityTypes.StockKeepingUnit;

    public async Task<SynchronizationHandlerResult> HandleAsync(
        SynchronizationRequest request,
        CancellationToken cancellationToken)
    {
        ReferenceSynchronizationResult result;
        if (!Guid.TryParse(request.ExternalId, out Guid externalRefKey) ||
            externalRefKey == Guid.Empty)
        {
            result = ReferenceSynchronizationResult.Failure(
                OneCReferenceType.StockKeepingUnit,
                Guid.Empty,
                ReferenceSynchronizationOutcome.PermanentFailure,
                ReferenceSynchronizationReasons.InvalidRequest,
                $"The synchronization request contains an invalid external identity: {request.ExternalId}.",
                retrySuitable: false);
        }
        else
        {
            result = await synchronizer.SynchronizeAsync(externalRefKey, cancellationToken);
        }

        LogCorrelation(request, result);
        return mapper.Map(result);
    }

    private void LogCorrelation(
        SynchronizationRequest request,
        ReferenceSynchronizationResult result) =>
        logger.LogInformation(
            "1С synchronization correlation: {SynchronizationRequestId} {EntityType} {ExternalId} {NotifiedDataVersion} {CurrentOutcome} {CurrentReason} {RetrySuitable}.",
            request.Id,
            EntityType,
            request.ExternalId,
            Convert.ToBase64String(request.ExternalDataVersion),
            result.Outcome,
            result.Reason,
            result.RetrySuitable);
}
