using Myrmex.AppDispatching.EventDispatching;
using Myrmex.Core.Events;

namespace Myrmex.Tests.Wms.Topology.Testing;

internal sealed class RecordingDomainEventDispatcher : IDomainEventDispatcher
{
    public List<IDomainEvent> DispatchedEvents { get; } = [];

    public Task DispatchAsync(
        IDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        DispatchedEvents.Add(domainEvent);

        return Task.CompletedTask;
    }

    public Task DispatchAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        DispatchedEvents.AddRange(domainEvents);

        return Task.CompletedTask;
    }
}