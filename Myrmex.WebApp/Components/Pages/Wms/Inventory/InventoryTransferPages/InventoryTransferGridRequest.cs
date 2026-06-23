namespace Myrmex.WebApp.Components.Pages.Wms.Inventory.InventoryTransferPages;

public sealed record InventoryTransferGridRequest(
    int Skip,
    int Take,
    string? SortBy,
    bool SortDescending);
