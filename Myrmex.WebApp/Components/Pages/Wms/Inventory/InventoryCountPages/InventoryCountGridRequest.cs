namespace Myrmex.WebApp.Components.Pages.Wms.Inventory.InventoryCountPages;

public sealed record InventoryCountGridRequest(
    int Skip,
    int Take,
    string? SortBy,
    bool SortDescending);
