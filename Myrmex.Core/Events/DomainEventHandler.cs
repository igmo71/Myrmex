namespace Myrmex.Core.Events;

public abstract class DomainEventHandler<TEvent> : IDomainEventHandler<TEvent>
    where TEvent : IDomainEvent
{
    public Type EventType => typeof(TEvent);

    public Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent is not TEvent typedDomainEvent)
        {
            throw new InvalidOperationException($"Handler '{GetType().Name}' cannot handle event '{domainEvent.GetType().Name}'.");
        }

        return HandleAsync(typedDomainEvent, cancellationToken);
    }

    public abstract Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}