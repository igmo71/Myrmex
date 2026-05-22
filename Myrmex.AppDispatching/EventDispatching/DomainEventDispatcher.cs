using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Myrmex.Core.Events;

namespace Myrmex.AppDispatching.EventDispatching;

internal sealed class DomainEventDispatcher(
    IServiceProvider serviceProvider,
    IDomainEventHandlerRegistry registry,
    ILogger<DomainEventDispatcher> logger)
    : IDomainEventDispatcher
{
    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        Type eventType = domainEvent.GetType();

        IReadOnlyList<Type> handlerTypes = registry.GetHandlerTypes(eventType);

        if (handlerTypes.Count == 0)
        {
            logger.LogDebug("No domain event handlers registered for event {EventType}.", eventType.Name);

            return;
        }

        foreach (Type handlerType in handlerTypes)
        {
            IDomainEventHandler handler = (IDomainEventHandler)serviceProvider.GetRequiredService(handlerType);

            try
            {
                await handler.HandleAsync(domainEvent, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Domain event handler {HandlerType} failed while handling domain event {EventType}.",
                    handlerType.Name, eventType.Name);

                throw;
            }
        }
    }

    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        foreach (IDomainEvent domainEvent in domainEvents)
        {
            await DispatchAsync(domainEvent, cancellationToken);
        }
    }
}

