namespace Myrmex.EventDispatching;

internal interface IDomainEventHandlerRegistry
{
    IReadOnlyList<Type> GetHandlerTypes(Type eventType);
}