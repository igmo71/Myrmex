namespace Myrmex.Shared.Wms.Receiving;

public sealed record ReceiveReceivingOrderLineRequest(
    decimal Quantity,
    string? ExpectedOrderVersion);
