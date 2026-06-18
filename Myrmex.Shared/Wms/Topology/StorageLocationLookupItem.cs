namespace Myrmex.Shared.Wms.Topology;

public sealed record StorageLocationLookupItem(
    Guid Id,
    Guid WarehouseId,
    string Code,
    string Name,
    bool IsActive);
