namespace Myrmex.Integrations.Synchronization;

internal enum SynchronizationRequestIntakeResultKind
{
    Inserted = 0,
    Duplicate = 1
}

internal sealed record SynchronizationRequestIntakeResult(
    SynchronizationRequest Request,
    SynchronizationRequestIntakeResultKind Kind);
