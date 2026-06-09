using Myrmex.Core.Events;

namespace Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;

internal sealed record UnitOfMeasureCreatedDomainEvent(Guid UnitOfMeasureId) : IDomainEvent;
