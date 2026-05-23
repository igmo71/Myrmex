using Myrmex.Core.Events;

namespace Myrmex.Modules.Wms.Topology.Domain.StorageLocations;

internal sealed record StorageLocationCreatedDomainEvent(
    Guid StorageLocationId,
    Guid WarehouseId,
    Guid ZoneId) : IDomainEvent;

internal sealed record StorageLocationDetailsUpdatedDomainEvent(
    Guid StorageLocationId,
    Guid WarehouseId,
    Guid ZoneId) : IDomainEvent;

internal sealed record StorageLocationStatusChangedDomainEvent(
    Guid StorageLocationId,
    Guid WarehouseId,
    Guid ZoneId,
    Guid StorageLocationStatusId) : IDomainEvent;

internal sealed record StorageLocationDeactivatedDomainEvent(
    Guid StorageLocationId,
    Guid WarehouseId,
    Guid ZoneId) : IDomainEvent;

internal sealed record StorageLocationReactivatedDomainEvent(
    Guid StorageLocationId,
    Guid WarehouseId,
    Guid ZoneId) : IDomainEvent;