namespace Myrmex.Shared.Wms.Topology;

public sealed record WarehouseLookupItem(
    Guid Id,
    string Code,
    string Name,
    bool IsActive);

