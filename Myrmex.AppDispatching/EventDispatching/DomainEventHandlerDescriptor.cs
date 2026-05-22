namespace Myrmex.AppDispatching.EventDispatching;

internal sealed record DomainEventHandlerDescriptor(Type EventType, Type HandlerType);
