namespace Myrmex.WebApp.Components.Pages.Wms.Catalog.SkuPages;

public sealed record SkuGridRequest(
    int Skip,
    int Take,
    string SortBy,
    bool SortDescending);

