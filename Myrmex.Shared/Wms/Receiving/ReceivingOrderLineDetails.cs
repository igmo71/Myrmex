namespace Myrmex.Shared.Wms.Receiving;

public sealed record ReceivingOrderLineDetails(
    Guid Id,
    ReceivingOrderLineDetails.StockKeepingUnitInfo Sku,
    decimal PlannedQuantity,
    decimal ReceivedQuantity,
    decimal RemainingQuantity)
{
    public sealed record StockKeepingUnitInfo(
        Guid Id,
        string Code,
        string Name,
        UnitOfMeasureInfo BaseUom);

    public sealed record UnitOfMeasureInfo(Guid Id, string Code, string? Symbol);
}
