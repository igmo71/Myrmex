using Microsoft.AspNetCore.Components;
using MudBlazor;
using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Topology;
using Myrmex.Shared.Wms.Inventory;
using Myrmex.WebApp.Wms.Api;
using Myrmex.WebApp.Wms.Inventory;
using Myrmex.WebApp.Wms.Topology;

namespace Myrmex.WebApp.Components.Pages.Wms.Inventory.InventoryCountPages;

public partial class Index
{
    private const int AutocompleteTake = 20;

    [Inject] private WmsInventoryApiClient WmsInventoryApiClient { get; set; } = default!;
    [Inject] private WmsTopologyApiClient WmsTopologyApiClient { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private InventoryCountGrid? _inventoryCountGrid;
    private WarehouseLookupItem? _selectedWarehouse;
    private Guid? _selectedWarehouseId;
    private string? _selectedStatus;
    private DateTime? _createdFrom;
    private DateTime? _createdTo;
    private string? _errorMessage;

    private async Task<GridData<InventoryCountListItem>> LoadInventoryCountsAsync(
        InventoryCountGridRequest gridRequest,
        CancellationToken cancellationToken)
    {
        _errorMessage = null;
        try
        {
            ListResult<InventoryCountListItem> result =
                await WmsInventoryApiClient.ListInventoryCountsAsync(
                    new ListInventoryCountsRequest
                    {
                        Skip = gridRequest.Skip,
                        Take = gridRequest.Take,
                        SortBy = gridRequest.SortBy,
                        SortDescending = gridRequest.SortDescending,
                        WarehouseId = _selectedWarehouseId,
                        Status = _selectedStatus,
                        CreatedFromUtc = ToStartOfDayUtc(_createdFrom),
                        CreatedToUtc = ToEndOfDayUtc(_createdTo)
                    },
                    cancellationToken);
            return new GridData<InventoryCountListItem>
            {
                Items = result.Items,
                TotalItems = result.TotalCount
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return EmptyGridData();
        }
        catch (Exception exception)
        {
            _errorMessage = exception.Message;
            await InvokeAsync(StateHasChanged);

            return EmptyGridData();
        }
    }

    private async Task<IEnumerable<WarehouseLookupItem>> SearchWarehousesAsync(
        string value,
        CancellationToken cancellationToken)
    {
        try
        {
            return await WmsTopologyApiClient.LookupWarehousesAsync(
                    new LookupWarehousesRequest
                    {
                        SearchText = value,
                        Take = AutocompleteTake,
                        SelectableOnly = true
                    },
                    cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return [];
        }
        catch (Exception exception)
        {
            _errorMessage = exception.Message;
            return [];
        }
    }

    private async Task CreateAsync()
    {
        IDialogReference dialog = await DialogService.ShowAsync<CreateInventoryCountDialog>(
            Localizer["InventoryCount.CreateTitle"],
            new DialogOptions { CloseButton = true, FullWidth = true, MaxWidth = MaxWidth.Small });
        DialogResult? result = await dialog.Result;
        if (result is not null &&
            !result.Canceled &&
            result.Data is InventoryCountDetails count)
        {
            NavigationManager.NavigateTo($"/wms/inventory/counts/{count.Id}");
        }
    }

    private void OpenAsync(InventoryCountListItem count) =>
        NavigationManager.NavigateTo($"/wms/inventory/counts/{count.Id}");

    private async Task CancelAsync(InventoryCountListItem count)
    {
        bool? confirmed = await DialogService.ShowMessageBoxAsync(
            Localizer["InventoryCount.CancelTitle"],
            Localizer["InventoryCount.CancelAppliedPrompt"],
            yesText: Localizer["InventoryCount.CancelCount"],
            cancelText: Localizer["InventoryCount.KeepOpen"]);
        if (confirmed != true)
        {
            return;
        }

        ApiResult<InventoryCountDetails> result =
            await WmsInventoryApiClient.TryCancelInventoryCountAsync(
                count.Id,
                new ChangeInventoryCountStatusRequest(count.CountVersion));
        if (result.IsFailure)
        {
            _errorMessage = result.Error?.Message ?? Localizer["InventoryCount.CancelError"];
        }

        await ReloadAsync();
    }

    private async Task OnWarehouseChanged(WarehouseLookupItem? value)
    {
        _selectedWarehouse = value;
        _selectedWarehouseId = value?.Id;
        await ResetAndReloadAsync();
    }

    private async Task OnStatusChanged(string? value)
    {
        _selectedStatus = value;
        await ResetAndReloadAsync();
    }

    private async Task OnCreatedFromChanged(DateTime? value)
    {
        _createdFrom = value;
        await ResetAndReloadAsync();
    }

    private async Task OnCreatedToChanged(DateTime? value)
    {
        _createdTo = value;
        await ResetAndReloadAsync();
    }

    private Task ReloadAsync() =>
        _inventoryCountGrid?.ReloadServerDataAsync() ?? Task.CompletedTask;

    private Task ResetAndReloadAsync() =>
        _inventoryCountGrid?.ResetAndReloadServerDataAsync() ?? Task.CompletedTask;

    private static DateTimeOffset? ToStartOfDayUtc(DateTime? date) =>
        date is null
            ? null
            : new DateTimeOffset(
                DateTime.SpecifyKind(date.Value.Date, DateTimeKind.Local))
                .ToUniversalTime();

    private static DateTimeOffset? ToEndOfDayUtc(DateTime? date) =>
        date is null
            ? null
            : new DateTimeOffset(
                DateTime.SpecifyKind(
                    date.Value.Date.AddDays(1).AddTicks(-1),
                    DateTimeKind.Local))
                .ToUniversalTime();

    private static GridData<InventoryCountListItem> EmptyGridData() =>
        new() { Items = [], TotalItems = 0 };
}
