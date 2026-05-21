namespace Myrmex.Core.Events;

public interface IDomainEventHandler<TEvent> : IEventHandler
    where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent appEvent, CancellationToken cancellationToken = default);
}
