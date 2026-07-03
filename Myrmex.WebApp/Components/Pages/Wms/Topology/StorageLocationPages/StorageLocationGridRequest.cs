namespace Myrmex.WebApp.Components.Pages.Wms.Topology.StorageLocationPages;

public sealed record StorageLocationGridRequest(
    int Skip,
    int Take,
    string SortBy,
    bool SortDescending);
