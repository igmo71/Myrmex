using Microsoft.AspNetCore.Components;
using MudBlazor;
using Myrmex.Shared.Wms.Catalog;
using Myrmex.Shared.Wms.Receiving;
using Myrmex.Shared.Wms.Topology;
using Myrmex.WebApp.Wms.Api;
using Myrmex.WebApp.Wms.Receiving;
using Myrmex.WebApp.Wms.Topology;

namespace Myrmex.WebApp.Components.Pages.Wms.Receiving.ReceivingOrderPages;

public partial class ReceivingOrderDraftPage
{
    [Inject] protected WmsTopologyApiClient TopologyApiClient { get; set; } = null!;
    [Inject] protected WmsReceivingApiClient ReceivingApiClient { get; set; } = null!;
    [Inject] protected IDialogService DialogService { get; set; } = null!;
    [Inject] protected NavigationManager Navigation { get; set; } = null!;

    protected string? Number { get; set; }
    protected WarehouseLookupItem? Warehouse { get; set; }
    protected StorageLocationLookupItem? Location { get; set; }
    protected List<DraftLine> Lines { get; } = [];
    protected bool Saving { get; set; }
    protected string? ErrorMessage { get; set; }

    protected Task<IEnumerable<WarehouseLookupItem>> SearchWarehousesAsync(string value, CancellationToken token) =>
        LookupWarehousesAsync(value, token);

    private async Task<IEnumerable<WarehouseLookupItem>> LookupWarehousesAsync(string value, CancellationToken token)
    {
        try { return await TopologyApiClient.LookupWarehousesAsync(new() { SearchText = value, Take = 20, SelectableOnly = true }, token); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { return []; }
        catch (Exception exception) { ErrorMessage = exception.Message; return []; }
    }

    protected Task WarehouseChangedAsync(WarehouseLookupItem? warehouse)
    {
        Warehouse = warehouse;
        Location = null;
        return Task.CompletedTask;
    }

    protected async Task<IEnumerable<StorageLocationLookupItem>> SearchLocationsAsync(string value, CancellationToken token)
    {
        if (Warehouse is null) return [];
        try
        {
            return await TopologyApiClient.LookupStorageLocationsAsync(Warehouse.Id, new()
            {
                SearchText = value, Take = 20, SelectableOnly = true,
                StorageLocationTypeCode = StorageLocationTypeCodes.Receiving
            }, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { return []; }
        catch (Exception exception) { ErrorMessage = exception.Message; return []; }
    }

    protected async Task AddSkuAsync()
    {
        DialogParameters parameters = new() { [nameof(SelectReceivingOrderSkuDialog.ExcludedSkuIds)] = Lines.Select(x => x.Sku.Id).ToArray() };
        IDialogReference dialog = await DialogService.ShowAsync<SelectReceivingOrderSkuDialog>(
            Localizer["Receiving.SelectSku"].Value, parameters, new() { CloseButton = true, FullWidth = true, MaxWidth = MaxWidth.Medium });
        DialogResult? result = await dialog.Result;
        if (result is { Canceled: false, Data: StockKeepingUnitLookupItem sku } && Lines.All(x => x.Sku.Id != sku.Id))
        {
            Lines.Add(new(sku, 1));
        }
    }

    protected void Remove(DraftLine line) => Lines.Remove(line);

    protected async Task SaveAsync()
    {
        if (Warehouse is null || Location is null || Lines.Count == 0 || Lines.Any(x => x.PlannedQuantity <= 0))
        {
            ErrorMessage = Localizer["Receiving.CreateValidation"];
            return;
        }
        Saving = true;
        ApiResult<ReceivingOrderDetails> result = await ReceivingApiClient.TryCreateReceivingOrderAsync(new(
            Number, Warehouse.Id, Location.Id,
            Lines.Select(x => new CreateReceivingOrderLineRequest(x.Sku.Id, x.PlannedQuantity)).ToList()));
        Saving = false;
        if (result.IsFailure || result.Value is null) { ErrorMessage = result.Error?.Message ?? Localizer["Receiving.CreateError"]; return; }
        Navigation.NavigateTo($"/wms/receiving-orders/{result.Value.Id}");
    }

    protected static string FormatWarehouse(WarehouseLookupItem? item) => item is null ? string.Empty : $"{item.Code} - {item.Name}";
    protected static string FormatLocation(StorageLocationLookupItem? item) => item is null ? string.Empty : $"{item.Code} - {item.Name}";
    protected sealed class DraftLine(StockKeepingUnitLookupItem sku, decimal plannedQuantity)
    {
        public StockKeepingUnitLookupItem Sku { get; } = sku;
        public decimal PlannedQuantity { get; set; } = plannedQuantity;
    }
}
