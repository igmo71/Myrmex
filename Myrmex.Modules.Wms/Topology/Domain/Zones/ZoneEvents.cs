using Myrmex.Core.Events;

namespace Myrmex.Modules.Wms.Topology.Domain.Zones;

public sealed record ZoneCreatedDomainEvent(Guid ZoneId, Guid WarehouseId) : IDomainEvent;

public sealed record ZoneDetailsUpdatedDomainEvent(Guid ZoneId, Guid WarehouseId) : IDomainEvent;

public sealed record ZoneDeactivatedDomainEvent(Guid ZoneId, Guid WarehouseId) : IDomainEvent;

public sealed record ZoneReactivatedDomainEvent(Guid ZoneId, Guid WarehouseId) : IDomainEvent;