namespace Myrmex.WebApp.Components.Pages.Wms.Inventory.InventoryLedgerPages;

public sealed record InventoryLedgerGridRequest(
    int Skip,
    int Take,
    string SortBy,
    bool SortDescending);
