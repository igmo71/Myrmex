namespace Myrmex.ApplicationDispatching.EventDispatching;

internal sealed record DomainEventHandlerDescriptor(Type EventType, Type HandlerType);
