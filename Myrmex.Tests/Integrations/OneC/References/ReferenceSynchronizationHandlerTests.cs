using Myrmex.Integrations.OneC.Common.References;
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
    internal void Mapper_MapsInternalOutcomeToExistingFeature104Result(
        ReferenceSynchronizationOutcome outcome,
        SynchronizationHandlerResultKind expectedKind)
    {
        ReferenceSynchronizationHandlerResultMapper mapper = new();
        SynchronizationHandlerResult result = mapper.Map(Result(Guid.NewGuid(), outcome));

        Assert.Equal(expectedKind, result.Kind);
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
}
