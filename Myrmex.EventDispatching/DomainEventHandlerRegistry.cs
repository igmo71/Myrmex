namespace Myrmex.EventDispatching;

internal sealed class DomainEventHandlerRegistry(IEnumerable<DomainEventHandlerDescriptor> descriptors)
    : IDomainEventHandlerRegistry
{
    private readonly IReadOnlyDictionary<Type, IReadOnlyList<Type>> _handlersByEventType =
        descriptors
            .GroupBy(descriptor => descriptor.EventType)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<Type>)group
                    .Select(descriptor => descriptor.HandlerType)
                    .Distinct()
                    .ToArray());

    public IReadOnlyList<Type> GetHandlerTypes(Type eventType)
    {
        return _handlersByEventType.TryGetValue(eventType, out IReadOnlyList<Type>? handlerTypes) ? handlerTypes : [];
    }
}