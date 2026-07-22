using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Receiving.Domain.ReceivingOrders;

namespace Myrmex.Modules.Wms.Receiving.Features.ReceivingOrders;

internal static class ReceivingOrderErrors
{
    public static ServiceError ConcurrencyConflict(string property) => new(
        ServiceErrorType.Conflict,
        "ReceivingOrder.ConcurrencyConflict",
        "Receiving order was changed by another operation. Refresh the order before retrying.",
        property);

    public static ServiceError InvalidState(string message, string? property = null) => new(
        ServiceErrorType.Conflict,
        "ReceivingOrder.InvalidState",
        message,
        property ?? nameof(ReceivingOrder.Status));

    public static ServiceError InventoryPostingConflict() => new(
        ServiceErrorType.Conflict,
        "ReceivingOrder.InventoryPostingConflict",
        "Inventory changed while the receiving order was being completed. Refresh before deciding whether to retry.",
        nameof(ReceivingOrder.InventoryTransactionId));

    public static ServiceError InvalidPersistedState(string message) => new(
        ServiceErrorType.Failure,
        "ReceivingOrder.InvalidPersistedState",
        message);

    public static ServiceError NumberConflict() => new(
        ServiceErrorType.Conflict,
        "ReceivingOrder.NumberConflict",
        "Receiving order Number already exists.",
        nameof(ReceivingOrder.Number));

    public static ServiceError ReceivingLocationInvalid(string message, string property) => new(
        ServiceErrorType.Invalid,
        "ReceivingOrder.ReceivingLocationInvalid",
        message,
        property);

    public static ServiceError DuplicateSku(string property) => new(
        ServiceErrorType.Invalid,
        "ReceivingOrderLine.DuplicateSku",
        "Each SKU may appear only once in a receiving order.",
        property);

    public static ServiceError ForeignLine(string property) => new(
        ServiceErrorType.Invalid,
        "ReceivingOrderLine.ForeignLine",
        "LineId does not belong to this receiving order.",
        property);

    public static ServiceError OverReceipt(string property) => new(
        ServiceErrorType.Conflict,
        "ReceivingOrderLine.OverReceipt",
        "Received quantity cannot exceed planned quantity.",
        property);
}
