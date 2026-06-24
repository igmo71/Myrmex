namespace Myrmex.Shared.Wms.Inventory;

public sealed record RecordInventoryCountLineRequest(
    decimal CountedQuantity,
    string? Comment,
    string? ExpectedLineVersion);
