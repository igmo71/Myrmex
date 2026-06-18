namespace Myrmex.Shared.Wms.Catalog;

public sealed record LookupStockKeepingUnitsRequest
{
    public string? SearchText { get; init; }

    public int? Take { get; init; }

    public bool SelectableOnly { get; init; } = true;
}
