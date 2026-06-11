using Myrmex.Core.Events;

namespace Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;

internal sealed record InventoryBalanceCreatedDomainEvent(
    Guid InventoryBalanceId,
    Guid StockKeepingUnitId,
    Guid StorageLocationId,
    decimal Quantity) : IDomainEvent;

internal sealed record InventoryBalanceQuantityUpdatedDomainEvent(
    Guid InventoryBalanceId,
    decimal Quantity) : IDomainEvent;
