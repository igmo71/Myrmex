using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryCounts;

namespace Myrmex.Modules.Wms.Inventory.Features.InventoryCounts;

internal static class InventoryCountErrors
{
    public static ServiceError CountConcurrency(string property) => new(
        ServiceErrorType.Conflict,
        "InventoryCount.ConcurrencyConflict",
        "Inventory count was changed by another operation. Refresh the count before retrying.",
        property);

    public static ServiceError LineConcurrency(string property) => new(
        ServiceErrorType.Conflict,
        "InventoryCountLine.ConcurrencyConflict",
        "Inventory count line was changed by another operation. Refresh the count before retrying.",
        property);

    public static ServiceError DuplicateLine() => new(
        ServiceErrorType.Conflict,
        "InventoryCountLine.DuplicateCurrentPair",
        "The SKU and storage location already have a current line in this count.",
        nameof(InventoryCountLine.StockKeepingUnitId));

    public static ServiceError InvalidState(string message, string property) => new(
        ServiceErrorType.Conflict,
        "InventoryCount.InvalidState",
        message,
        property);
}
