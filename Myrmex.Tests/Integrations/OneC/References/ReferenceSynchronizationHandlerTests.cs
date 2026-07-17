using Myrmex.Integrations.OneC.References;
using Myrmex.Integrations.Synchronization;
using Myrmex.Integrations.Synchronization.Processing;

namespace Myrmex.Tests.Integrations.OneC.References;

public sealed class ReferenceSynchronizationHandlerTests
{
    [Theory]
    [InlineData(ReferenceSynchronizationOutcome.Applied, SynchronizationHandlerResultKind.Completed)]
    [InlineData(ReferenceSynchronizationOutcome.Unchanged, SynchronizationHandlerResultKind.Completed)]
    [InlineData(ReferenceSynchronizationOutcome.ControlledSkip, SynchronizationHandlerResultKind.Completed)]
    [InlineData(ReferenceSynchronizationOutcome.Busy, SynchronizationHandlerResultKind.TransientFailure)]
    [InlineData(ReferenceSynchronizationOutcome.TransientFailure, SynchronizationHandlerResultKind.TransientFailure)]
    [InlineData(ReferenceSynchronizationOutcome.NotFound, SynchronizationHandlerResultKind.PermanentFailure)]
    [InlineData(ReferenceSynchronizationOutcome.PermanentFailure, SynchronizationHandlerResultKind.PermanentFailure)]
    internal async Task Handler_MapsInternalOutcomeToExistingFeature104Result(
        ReferenceSynchronizationOutcome outcome,
        SynchronizationHandlerResultKind expectedKind)
    {
        Guid externalRefKey = Guid.NewGuid();
        StubSynchronizationService service = new(Result(externalRefKey, outcome));
        WarehouseReferenceSynchronizationHandler handler = new(service);
        SynchronizationRequest request = new()
        {
            SourceSystem = "OneC",
            SourceInstance = "main",
            EntityType = SynchronizationEntityTypes.Warehouse,
            ExternalId = externalRefKey.ToString("D"),
            ExternalDataVersion = [1],
            ReceivedAtUtc = DateTimeOffset.UtcNow
        };

        SynchronizationHandlerResult result = await handler.HandleAsync(
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(expectedKind, result.Kind);
        Assert.Equal(externalRefKey, service.ExternalRefKey);
        Assert.DoesNotContain(
            nameof(ReferenceSynchronizationOutcome.NotFound),
            Enum.GetNames<SynchronizationStatus>());
    }

    private static ReferenceSynchronizationResult Result(
        Guid externalRefKey,
        ReferenceSynchronizationOutcome outcome) =>
        outcome is ReferenceSynchronizationOutcome.Applied or
            ReferenceSynchronizationOutcome.Unchanged or
            ReferenceSynchronizationOutcome.ControlledSkip
            ? ReferenceSynchronizationResult.Success(
                OneCReferenceType.Warehouse,
                externalRefKey,
                outcome,
                outcome.ToString())
            : ReferenceSynchronizationResult.Failure(
                OneCReferenceType.Warehouse,
                externalRefKey,
                outcome,
                outcome.ToString(),
                "Expected diagnostic.",
                retrySuitable: outcome is ReferenceSynchronizationOutcome.Busy or
                    ReferenceSynchronizationOutcome.TransientFailure);

    private sealed class StubSynchronizationService(ReferenceSynchronizationResult result)
        : IOneCReferenceSynchronizationService
    {
        public Guid? ExternalRefKey { get; private set; }

        public Task<ReferenceSynchronizationResult> SynchronizeAsync(
            OneCReferenceType referenceType,
            Guid externalRefKey,
            CancellationToken cancellationToken) =>
            Task.FromResult(result);

        public Task<ReferenceSynchronizationResult> SynchronizeWarehouseAsync(
            Guid externalRefKey,
            CancellationToken cancellationToken)
        {
            ExternalRefKey = externalRefKey;
            return Task.FromResult(result);
        }

        public Task<ReferenceSynchronizationResult> SynchronizeUnitOfMeasureAsync(
            Guid externalRefKey,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<ReferenceSynchronizationResult> SynchronizeStockKeepingUnitAsync(
            Guid externalRefKey,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
