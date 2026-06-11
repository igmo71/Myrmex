using Myrmex.Core.Domain.Validation;

namespace Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;

internal static class InventoryBalanceValidationErrors
{
    public static DomainValidationFailure StockKeepingUnitIdRequired => new(
        "InventoryBalance.StockKeepingUnitIdRequired",
        "SKU is required.",
        "stockKeepingUnitId");

    public static DomainValidationFailure StorageLocationIdRequired => new(
        "InventoryBalance.StorageLocationIdRequired",
        "Storage location is required.",
        "storageLocationId");

    public static DomainValidationFailure QuantityMustBeNonNegative => new(
        "InventoryBalance.QuantityMustBeNonNegative",
        "Inventory balance quantity must be greater than or equal to zero.",
        "quantity");
}
