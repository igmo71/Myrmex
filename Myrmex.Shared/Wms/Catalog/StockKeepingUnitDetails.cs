namespace Myrmex.Shared.Wms.Catalog;

public sealed record StockKeepingUnitDetails(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    Guid BaseUnitOfMeasureId,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

