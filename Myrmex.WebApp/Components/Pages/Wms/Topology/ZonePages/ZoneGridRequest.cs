namespace Myrmex.WebApp.Components.Pages.Wms.Topology.ZonePages;

public sealed record ZoneGridRequest(
    int Skip,
    int Take,
    string SortBy,
    bool SortDescending);
