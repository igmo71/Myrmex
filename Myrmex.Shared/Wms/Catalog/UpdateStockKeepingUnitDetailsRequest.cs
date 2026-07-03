namespace Myrmex.Shared.Wms.Catalog;

public sealed record UpdateStockKeepingUnitDetailsRequest(
    string? Name,
    string? Description,
    Guid? BaseUnitOfMeasureId = null);

