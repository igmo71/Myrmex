using Myrmex.Core.Events;

namespace Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;

internal sealed record StockKeepingUnitCreatedDomainEvent(Guid StockKeepingUnitId) : IDomainEvent;
