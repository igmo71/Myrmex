namespace Myrmex.Core.Events;

public interface IDomainEventHandler : IEventHandler
{
    Type EventType { get; }

    Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}

public interface IDomainEventHandler<in TEvent> : IDomainEventHandler
    where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
