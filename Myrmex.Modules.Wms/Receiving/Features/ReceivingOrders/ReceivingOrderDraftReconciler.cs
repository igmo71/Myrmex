using Myrmex.Core.Domain.Validation;
using Myrmex.Modules.Wms.Receiving.Domain.ReceivingOrders;

namespace Myrmex.Modules.Wms.Receiving.Features.ReceivingOrders;

internal static class ReceivingOrderDraftReconciler
{
    public static DomainValidationResult Replace(
        ReceivingOrder order,
        string number,
        Guid warehouseId,
        Guid receivingLocationId,
        IEnumerable<ReceivingOrder.DraftLine> lines,
        out IReadOnlyList<ReceivingOrderLine> removedLines) =>
        order.ReplaceDraft(number, warehouseId, receivingLocationId, lines, out removedLines);
}
