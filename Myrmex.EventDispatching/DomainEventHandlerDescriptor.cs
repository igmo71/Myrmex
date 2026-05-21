namespace Myrmex.EventDispatching;

internal sealed record DomainEventHandlerDescriptor(Type EventType, Type HandlerType);