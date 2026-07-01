namespace Myrmex.WebApp.Components.Pages.Wms.Topology.WarehousePages;

public sealed record WarehouseGridRequest(
    int Skip,
    int Take,
    string SortBy,
    bool SortDescending);
