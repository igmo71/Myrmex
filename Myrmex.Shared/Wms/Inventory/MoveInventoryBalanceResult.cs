namespace Myrmex.Shared.Wms.Inventory;

public sealed record MoveInventoryBalanceResult(
    InventoryBalanceDetails SourceBalance,
    InventoryBalanceDetails DestinationBalance,
    decimal MovedQuantity,
    decimal SourceQuantityBefore,
    decimal SourceQuantityAfter,
    decimal DestinationQuantityBefore,
    decimal DestinationQuantityAfter,
    DateTimeOffset OccurredAtUtc);
