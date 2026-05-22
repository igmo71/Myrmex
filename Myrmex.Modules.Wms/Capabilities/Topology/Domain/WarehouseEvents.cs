using Myrmex.Core.Events;

namespace Myrmex.Modules.Wms.Capabilities.Topology.Domain;

internal sealed record WarehouseCreatedDomainEvent(Guid WarehouseId) : IDomainEvent;

internal sealed record WarehouseDetailsUpdatedDomainEvent(Guid WarehouseId) : IDomainEvent;

internal sealed record WarehouseDeactivatedDomainEvent(Guid WarehouseId) : IDomainEvent;

internal sealed record WarehouseReactivatedDomainEvent(Guid WarehouseId) : IDomainEvent;
