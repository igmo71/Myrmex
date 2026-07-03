namespace Myrmex.Shared.Wms.Catalog;

public sealed record UnitOfMeasureDetails(
    Guid Id,
    string Code,
    string Name,
    string? Symbol,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

