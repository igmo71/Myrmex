namespace Myrmex.Shared.Wms.Topology;

public sealed record StorageLocationTypeDetails(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsSystem,
    bool IsActive,
    int SortOrder);

