using Myrmex.Core.Events;

namespace Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;

internal sealed record UnitOfMeasureCreatedDomainEvent(Guid UnitOfMeasureId) : IDomainEvent;

internal sealed record UnitOfMeasureDetailsUpdatedDomainEvent(Guid UnitOfMeasureId) : IDomainEvent;

internal sealed record UnitOfMeasureDeactivatedDomainEvent(Guid UnitOfMeasureId) : IDomainEvent;

internal sealed record UnitOfMeasureReactivatedDomainEvent(Guid UnitOfMeasureId) : IDomainEvent;
