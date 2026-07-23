namespace Myrmex.WebApp.Components.Pages.Wms.Receiving.ReceivingOrderPages;

public sealed record ReceivingOrderGridRequest(
    int Skip,
    int Take,
    string? SortBy,
    bool SortDescending);
