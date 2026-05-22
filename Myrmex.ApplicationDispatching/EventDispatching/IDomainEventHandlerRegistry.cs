namespace Myrmex.ApplicationDispatching.EventDispatching;

internal interface IDomainEventHandlerRegistry
{
    IReadOnlyList<Type> GetHandlerTypes(Type eventType);
}
