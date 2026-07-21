using Myrmex.Integrations.Synchronization.Processing;

namespace Myrmex.Integrations.OneC.Common.References;

internal sealed class ReferenceSynchronizationHandlerResultMapper
{
    public SynchronizationHandlerResult Map(ReferenceSynchronizationResult result) =>
        result.Outcome switch
        {
            ReferenceSynchronizationOutcome.Applied or
            ReferenceSynchronizationOutcome.Unchanged or
            ReferenceSynchronizationOutcome.ControlledSkip =>
                SynchronizationHandlerResult.Completed(),

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
