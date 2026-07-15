namespace Myrmex.Integrations.Synchronization.Processing;

internal interface ISynchronizationHandler
{
    string EntityType { get; }

    Task<SynchronizationHandlerResult> HandleAsync(
        SynchronizationRequest request,
        CancellationToken cancellationToken);
}

internal enum SynchronizationHandlerResultKind
{
    Completed = 0,
    TransientFailure = 1,
    PermanentFailure = 2
}

internal sealed record SynchronizationHandlerResult(
    SynchronizationHandlerResultKind Kind,
    string? Error)
{
    public static SynchronizationHandlerResult Completed() =>
        new(SynchronizationHandlerResultKind.Completed, Error: null);

    public static SynchronizationHandlerResult TransientFailure(
        string error) =>
        new(SynchronizationHandlerResultKind.TransientFailure, error);

    public static SynchronizationHandlerResult PermanentFailure(
        string error) =>
        new(SynchronizationHandlerResultKind.PermanentFailure, error);
}
