namespace Myrmex.Shared.Wms.Catalog;

public sealed record CreateStockKeepingUnitRequest(
    string? Code,
    string? Name,
    string? Description,
    Guid? BaseUnitOfMeasureId = null);

