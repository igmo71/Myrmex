namespace Myrmex.Integrations.Synchronization.Processing;

internal interface ISynchronizationHandlerResolver
{
    ISynchronizationHandler? Resolve(string entityType);
}

internal sealed class SynchronizationHandlerResolver(
    IEnumerable<ISynchronizationHandler> handlers)
    : ISynchronizationHandlerResolver
{
    private readonly IReadOnlyDictionary<string, ISynchronizationHandler> _handlers =
        handlers.ToDictionary(
            handler => handler.EntityType,
            StringComparer.Ordinal);

    public ISynchronizationHandler? Resolve(string entityType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);

        return _handlers.GetValueOrDefault(entityType);
    }
}
