namespace Myrmex.Core.Abstractions;

public interface IIntegrationEventHandler<TEvent> : IEventHandler
    where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent integrationEvent);
}
