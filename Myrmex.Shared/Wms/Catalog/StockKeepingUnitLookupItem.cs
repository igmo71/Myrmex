namespace Myrmex.Shared.Wms.Catalog;

public sealed record StockKeepingUnitLookupItem(
    Guid Id,
    string Code,
    string Name,
    Guid BaseUnitOfMeasureId,
    string BaseUnitOfMeasureCode,
    string? BaseUnitOfMeasureSymbol,
    bool IsActive,
    bool IsBaseUnitOfMeasureActive);
