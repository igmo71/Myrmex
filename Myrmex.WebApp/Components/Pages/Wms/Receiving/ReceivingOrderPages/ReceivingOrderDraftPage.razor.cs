using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using MudBlazor;
using Myrmex.Shared.Wms.Catalog;
using Myrmex.Shared.Wms.Receiving;
using Myrmex.Shared.Wms.Topology;
using Myrmex.WebApp.Localization;
using Myrmex.WebApp.Wms.Api;
using Myrmex.WebApp.Wms.Receiving;
using Myrmex.WebApp.Wms.Topology;

namespace Myrmex.WebApp.Components.Pages.Wms.Receiving.ReceivingOrderPages;

public partial class ReceivingOrderDraftPage
{
    private const string ConcurrencyConflictCode = "ReceivingOrder.ConcurrencyConflict";
    private const string InvalidStateCode = "ReceivingOrder.InvalidState";

    [Parameter] public Guid? ReceivingOrderId { get; set; }
    [Inject] protected WmsTopologyApiClient TopologyApiClient { get; set; } = null!;
    [Inject] protected WmsReceivingApiClient ReceivingApiClient { get; set; } = null!;
    [Inject] protected IDialogService DialogService { get; set; } = null!;
    [Inject] protected NavigationManager Navigation { get; set; } = null!;
    [Inject] protected IStringLocalizer<SharedResource> Localizer { get; set; } = null!;

    protected string? Number { get; set; }
    protected WarehouseLookupItem? Warehouse { get; set; }
    protected StorageLocationLookupItem? Location { get; set; }
    protected List<DraftLine> Lines { get; } = [];
    protected bool Loading { get; set; }
    protected bool Saving { get; set; }
    protected bool SaveBlockedByConflict { get; set; }
    protected string? ErrorMessage { get; set; }
    protected string? LineSearchText { get; set; }
    private string? ExpectedOrderVersion { get; set; }
    private Guid? _loadedOrderId;

    protected bool IsEditMode => ReceivingOrderId.HasValue && ReceivingOrderId.Value != Guid.Empty;
    protected IEnumerable<DraftLine> VisibleLines
    {
        get
        {
            string searchText = LineSearchText?.Trim() ?? string.Empty;
            return searchText.Length == 0
                ? Lines
                : Lines.Where(line =>
                    line.Sku.Code.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    line.Sku.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }
    }
    protected bool CanSave => !IsEditMode || !string.IsNullOrWhiteSpace(ExpectedOrderVersion);
    protected string PageTitleText => IsEditMode
        ? Localizer["Receiving.EditTitle"]
        : Localizer["Receiving.CreateTitle"];
    protected string SaveButtonText => IsEditMode
        ? Localizer["Common.Save"]
        : Localizer["Common.Create"];

    protected override async Task OnParametersSetAsync()
    {
        if (IsEditMode && _loadedOrderId != ReceivingOrderId)
        {
            await LoadLatestAsync();
        }
        else if (!IsEditMode && _loadedOrderId.HasValue)
        {
            Number = null;
            Warehouse = null;
            Location = null;
            ExpectedOrderVersion = null;
            Lines.Clear();
            LineSearchText = null;
            SaveBlockedByConflict = false;
            ErrorMessage = null;
            _loadedOrderId = null;
        }
    }

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
                SearchText = value,
                Take = 20,
                SelectableOnly = true,
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
            Lines.Add(new(null, sku, 1));
        }
    }

    protected async Task ChangeSkuAsync(DraftLine line)
    {
        DialogParameters parameters = new()
        {
            [nameof(SelectReceivingOrderSkuDialog.ExcludedSkuIds)] = Lines
                .Where(candidate => !ReferenceEquals(candidate, line))
                .Select(candidate => candidate.Sku.Id)
                .ToArray()
        };
        IDialogReference dialog = await DialogService.ShowAsync<SelectReceivingOrderSkuDialog>(
            Localizer["Receiving.SelectSku"].Value,
            parameters,
            new() { CloseButton = true, FullWidth = true, MaxWidth = MaxWidth.Medium });
        DialogResult? result = await dialog.Result;
        if (result is { Canceled: false, Data: StockKeepingUnitLookupItem sku } &&
            Lines.Where(candidate => !ReferenceEquals(candidate, line)).All(candidate => candidate.Sku.Id != sku.Id))
        {
            line.Sku = sku;
        }
    }

    protected void Remove(DraftLine line) => Lines.Remove(line);

    protected async Task SaveAsync()
    {
        if (SaveBlockedByConflict)
        {
            return;
        }

        if (Warehouse is null || Location is null || Lines.Count == 0 || Lines.Any(x => x.PlannedQuantity <= 0))
        {
            ErrorMessage = Localizer["Receiving.CreateValidation"];
            return;
        }
        Saving = true;
        ErrorMessage = null;
        try
        {
            ApiResult<ReceivingOrderDetails> result = IsEditMode
                ? await ReceivingApiClient.TryUpdateReceivingOrderDraftAsync(
                    ReceivingOrderId!.Value,
                    new UpdateReceivingOrderDraftRequest(
                        Number,
                        Warehouse.Id,
                        Location.Id,
                        ExpectedOrderVersion,
                        Lines.Select(line => new UpdateReceivingOrderLineRequest(
                            line.LineId,
                            line.Sku.Id,
                            line.PlannedQuantity)).ToList()))
                : await ReceivingApiClient.TryCreateReceivingOrderAsync(new(
                    Number,
                    Warehouse.Id,
                    Location.Id,
                    Lines.Select(line => new CreateReceivingOrderLineRequest(
                        line.Sku.Id,
                        line.PlannedQuantity)).ToList()));

            if (result.IsFailure || result.Value is null)
            {
                if (IsEditMode && IsStaleDraftConflict(result.Error))
                {
                    SaveBlockedByConflict = true;
                    ErrorMessage = null;
                    return;
                }

                ErrorMessage = result.Error?.Message ?? Localizer[IsEditMode
                    ? "Receiving.UpdateError"
                    : "Receiving.CreateError"];
                return;
            }

            Navigation.NavigateTo($"/wms/receiving-orders/{result.Value.Id}");
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
        finally
        {
            Saving = false;
        }
    }

    private static bool IsStaleDraftConflict(ApiError? error) =>
        error is
        {
            Status: StatusCodes.Status409Conflict,
            Code: ConcurrencyConflictCode or InvalidStateCode
        };

    protected async Task ReloadLatestAsync()
    {
        bool? confirmed = await DialogService.ShowMessageBoxAsync(
            Localizer["Receiving.ReloadLatestTitle"].Value,
            Localizer["Receiving.ReloadLatestPrompt"].Value,
            yesText: Localizer["Receiving.ReloadLatestDiscard"].Value,
            cancelText: Localizer["Common.Cancel"].Value);
        if (confirmed == true)
        {
            await LoadLatestAsync();
        }
    }

    private async Task LoadLatestAsync()
    {
        if (!IsEditMode)
        {
            return;
        }

        Loading = true;
        ErrorMessage = null;
        ExpectedOrderVersion = null;
        try
        {
            ReceivingOrderDetails order = await ReceivingApiClient.GetReceivingOrderByIdAsync(
                ReceivingOrderId!.Value);
            if (order.Status != ReceivingOrderStatusDetails.Draft)
            {
                ErrorMessage = Localizer["Receiving.EditDraftOnly"];
                return;
            }

            Number = order.Number;
            Warehouse = new(order.Warehouse.Id, order.Warehouse.Code, order.Warehouse.Name, true);
            Location = new(
                order.ReceivingLocation.Id,
                order.Warehouse.Id,
                order.ReceivingLocation.Code,
                order.ReceivingLocation.Name,
                true);
            ExpectedOrderVersion = order.OrderVersion;
            Lines.Clear();
            Lines.AddRange(order.Lines.Select(line => new DraftLine(
                line.Id,
                new StockKeepingUnitLookupItem(
                    line.Sku.Id,
                    line.Sku.Code,
                    line.Sku.Name,
                    line.Sku.BaseUom.Id,
                    line.Sku.BaseUom.Code,
                    line.Sku.BaseUom.Symbol,
                    true,
                    true),
                line.PlannedQuantity)));
            LineSearchText = null;
            SaveBlockedByConflict = false;
            _loadedOrderId = order.Id;
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
        finally
        {
            Loading = false;
        }
    }

    protected static string FormatWarehouse(WarehouseLookupItem? item) => item is null ? string.Empty : item.Name;
    protected static string FormatLocation(StorageLocationLookupItem? item) => item is null ? string.Empty : $"{item.Code} - {item.Name}";
    protected static string FormatBaseUnitOfMeasure(StockKeepingUnitLookupItem sku) =>
        string.IsNullOrWhiteSpace(sku.BaseUnitOfMeasureSymbol)
            ? sku.BaseUnitOfMeasureCode
            : sku.BaseUnitOfMeasureSymbol;

    protected sealed class DraftLine(Guid? lineId, StockKeepingUnitLookupItem sku, decimal plannedQuantity)
    {
        public Guid? LineId { get; } = lineId;
        public StockKeepingUnitLookupItem Sku { get; set; } = sku;
        public decimal PlannedQuantity { get; set; } = plannedQuantity;
    }

    protected string? ValidatePositiveQuantity(decimal quantity) =>
        quantity > 0 ? null : Localizer["Common.QuantityMustBePositive"].Value;
}
