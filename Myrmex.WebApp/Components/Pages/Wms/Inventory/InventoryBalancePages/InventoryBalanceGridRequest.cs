namespace Myrmex.WebApp.Components.Pages.Wms.Inventory.InventoryBalancePages;

public sealed record InventoryBalanceGridRequest(
        int Skip,
        int Take,
        string SortBy,
        bool SortDescending);
