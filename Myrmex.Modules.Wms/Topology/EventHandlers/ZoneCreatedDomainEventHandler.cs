using Microsoft.Extensions.Logging;
using Myrmex.Core.Events;
using Myrmex.Modules.Wms.Topology.Domain.Zones;

namespace Myrmex.Modules.Wms.Topology.EventHandlers;

internal sealed class ZoneCreatedDomainEventHandler(
    ILogger<ZoneCreatedDomainEventHandler> logger)
    : DomainEventHandler<ZoneCreatedDomainEvent>
{
    public override Task HandleAsync(
        ZoneCreatedDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "Zone created domain event handled. ZoneId: {ZoneId}, WarehouseId: {WarehouseId}",
            domainEvent.ZoneId, domainEvent.WarehouseId);

        return Task.CompletedTask;
    }
}