namespace Myrmex.Shared.Wms.Catalog;

public sealed record CreateUnitOfMeasureRequest(
    string? Code,
    string? Name,
    string? Symbol);

