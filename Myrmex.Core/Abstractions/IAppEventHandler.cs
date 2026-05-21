namespace Myrmex.Core.Abstractions;

public interface IAppEventHandler<TEvent> : IEventHandler
    where TEvent : IAppEvent
{
    Task HandleAsync(TEvent appEvent);
}
