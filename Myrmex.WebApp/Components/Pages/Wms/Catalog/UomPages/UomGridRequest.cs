namespace Myrmex.WebApp.Components.Pages.Wms.Catalog.UomPages;

public sealed record UomGridRequest(
    int Skip,
    int Take,
    string SortBy,
    bool SortDescending);

