using Myrmex.Core.Events;

namespace Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;

internal sealed record StockKeepingUnitCreatedDomainEvent(Guid StockKeepingUnitId) : IDomainEvent;

internal sealed record StockKeepingUnitDetailsUpdatedDomainEvent(Guid StockKeepingUnitId) : IDomainEvent;

internal sealed record StockKeepingUnitDeactivatedDomainEvent(Guid StockKeepingUnitId) : IDomainEvent;

internal sealed record StockKeepingUnitReactivatedDomainEvent(Guid StockKeepingUnitId) : IDomainEvent;
