namespace Myrmex.AppDispatching.EventDispatching;

internal interface IDomainEventHandlerRegistry
{
    IReadOnlyList<Type> GetHandlerTypes(Type eventType);
}

