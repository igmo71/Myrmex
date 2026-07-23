using Microsoft.AspNetCore.Components;
using MudBlazor;
using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Receiving;
using Myrmex.Shared.Wms.Topology;
using Myrmex.WebApp.Wms.Api;
using Myrmex.WebApp.Wms.Receiving;
using Myrmex.WebApp.Wms.Topology;

namespace Myrmex.WebApp.Components.Pages.Wms.Receiving.ReceivingOrderPages;

public partial class Index
{
    private const int AutocompleteTake = 20;

    [Inject] private WmsReceivingApiClient ReceivingApiClient { get; set; } = default!;
    [Inject] private WmsTopologyApiClient TopologyApiClient { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private ReceivingOrderGrid? _receivingOrderGrid;
    private WarehouseLookupItem? _selectedWarehouse;
    private string? _selectedStatus;
    private string? _searchText;
    private string? _errorMessage;
    private string? _message;
    private Guid? _busyOrderId;

    private async Task<GridData<ReceivingOrderListItem>> LoadReceivingOrdersAsync(
        ReceivingOrderGridRequest gridRequest,
        CancellationToken cancellationToken)
    {
        _errorMessage = null;
        try
        {
            ListResult<ReceivingOrderListItem> result =
                await ReceivingApiClient.ListReceivingOrdersAsync(
                    new(
                        gridRequest.Skip,
                        gridRequest.Take,
                        _searchText,
                        _selectedWarehouse?.Id,
                        _selectedStatus,
                        gridRequest.SortBy,
                        gridRequest.SortDescending),
                    cancellationToken);
            return new GridData<ReceivingOrderListItem>
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
            return await TopologyApiClient.LookupWarehousesAsync(
                new()
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

    private void Create() => Navigation.NavigateTo("/wms/receiving-orders/new");

    private void Open(ReceivingOrderListItem order) =>
        Navigation.NavigateTo($"/wms/receiving-orders/{order.Id}");

    private void Edit(ReceivingOrderListItem order)
    {
        if (order.Status == ReceivingOrderStatusDetails.Draft)
        {
            Navigation.NavigateTo($"/wms/receiving-orders/{order.Id}/edit");
        }
    }

    private async Task DeleteAsync(ReceivingOrderListItem order)
    {
        if (order.Status != ReceivingOrderStatusDetails.Draft)
        {
            return;
        }

        bool? confirmed = await DialogService.ShowMessageBoxAsync(
            Localizer["Receiving.DeleteTitle"].Value,
            Localizer["Receiving.DeletePrompt", order.Number].Value,
            yesText: Localizer["Receiving.DeleteAction"].Value,
            cancelText: Localizer["Common.Cancel"].Value);
        if (confirmed != true)
        {
            return;
        }

        _errorMessage = null;
        _message = null;
        _busyOrderId = order.Id;
        try
        {
            ApiResult<bool> result = await ReceivingApiClient.TryDeleteReceivingOrderDraftAsync(
                order.Id,
                order.OrderVersion);
            if (result.IsFailure)
            {
                if (result.Error?.Status == StatusCodes.Status409Conflict)
                {
                    _message = Localizer["Receiving.DeleteConflictReloaded"];
                }
                else
                {
                    _errorMessage = result.Error?.Message ?? Localizer["Receiving.DeleteError"];
                }

                await ReloadAsync();
                return;
            }

            _message = Localizer["Receiving.Deleted"];
            await ReloadAsync();
        }
        catch (Exception exception)
        {
            _errorMessage = exception.Message;
        }
        finally
        {
            _busyOrderId = null;
        }
    }

    private async Task OnWarehouseChanged(WarehouseLookupItem? value)
    {
        _selectedWarehouse = value;
        await ResetAndReloadAsync();
    }

    private async Task OnStatusChanged(string? value)
    {
        _selectedStatus = value;
        await ResetAndReloadAsync();
    }

    private async Task OnSearchTextChanged(string? value)
    {
        _searchText = value;
        await ResetAndReloadAsync();
    }

    private Task ReloadAsync() =>
        _receivingOrderGrid?.ReloadServerDataAsync() ?? Task.CompletedTask;

    private Task ResetAndReloadAsync() =>
        _receivingOrderGrid?.ResetAndReloadServerDataAsync() ?? Task.CompletedTask;

    private static GridData<ReceivingOrderListItem> EmptyGridData() =>
        new() { Items = [], TotalItems = 0 };
}
