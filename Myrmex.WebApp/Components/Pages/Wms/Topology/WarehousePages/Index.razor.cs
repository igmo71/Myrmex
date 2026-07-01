using Microsoft.AspNetCore.Components;
using MudBlazor;
using Myrmex.Shared.Common;
using Myrmex.WebApp.Wms.Api;
using Myrmex.WebApp.Wms.Topology;

namespace Myrmex.WebApp.Components.Pages.Wms.Topology.WarehousePages;

public partial class Index
{
    [Inject]
    private WmsTopologyApiClient WmsTopologyApiClient { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    private WarehouseGrid? _warehouseGrid;
    private string? _errorMessage;
    private string? _searchText;
    private bool _includeInactive;

    private Task ReloadAsync()
    {
        return _warehouseGrid?.ReloadServerDataAsync() ?? Task.CompletedTask;
    }

    private async Task OnSearchTextChanged(string? value)
    {
        _searchText = value;
        await ResetAndReloadWarehousesAsync();
    }

    private async Task OnIncludeInactiveChanged(bool value)
    {
        _includeInactive = value;
        await ResetAndReloadWarehousesAsync();
    }

    private async Task<GridData<WarehouseDetails>> LoadWarehousesAsync(
        WarehouseGridRequest gridRequest,
        CancellationToken cancellationToken)
    {
        _errorMessage = null;

        try
        {
            ListRequest request = new(
                Skip: gridRequest.Skip,
                Take: gridRequest.Take,
                SearchText: _searchText,
                SortBy: gridRequest.SortBy,
                SortDescending: gridRequest.SortDescending,
                IncludeInactive: _includeInactive);

            ListResult<WarehouseDetails> result = await WmsTopologyApiClient
                .ListWarehousesAsync(request, cancellationToken);

            return new GridData<WarehouseDetails>
            {
                Items = result.Items,
                TotalItems = result.TotalCount
            };
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            return EmptyGridData();
        }
        catch (Exception exception)
        {
            _errorMessage = exception.Message;
            return EmptyGridData();
        }
    }

    private Task ResetAndReloadWarehousesAsync()
    {
        return _warehouseGrid?.ResetAndReloadServerDataAsync() ?? Task.CompletedTask;
    }

    private async Task CreateWarehouseAsync()
    {
        DialogOptions options = new()
        {
            CloseButton = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        IDialogReference dialog = await DialogService
            .ShowAsync<WarehouseEditDialog>(Localizer["Warehouse.CreateTitle"], options);

        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        Snackbar.Add(Localizer["Warehouse.Created"], Severity.Success);

        await ReloadAsync();
    }

    private async Task EditWarehouseAsync(WarehouseDetails warehouse)
    {
        DialogParameters parameters = new()
        {
            [nameof(WarehouseEditDialog.Warehouse)] = warehouse
        };

        DialogOptions options = new()
        {
            CloseButton = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        IDialogReference dialog = await DialogService
            .ShowAsync<WarehouseEditDialog>(Localizer["Warehouse.EditTitle"], parameters, options);

        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        Snackbar.Add(Localizer["Warehouse.Updated"], Severity.Success);

        await ReloadAsync();
    }

    private async Task DeactivateWarehouseAsync(WarehouseDetails warehouse)
    {
        try
        {
            ApiResult<WarehouseDetails> result = await WmsTopologyApiClient
                .TryDeactivateWarehouseAsync(warehouse.Id);

            if (result.IsFailure)
            {
                Snackbar.Add(result.Error?.Message ?? Localizer["Warehouse.DeactivateError"], Severity.Error);

                return;
            }

            Snackbar.Add(Localizer["Warehouse.Deactivated"], Severity.Success);

            await ReloadAsync();
        }
        catch (Exception exception)
        {
            Snackbar.Add(exception.Message, Severity.Error);
        }
    }

    private async Task ReactivateWarehouseAsync(WarehouseDetails warehouse)
    {
        try
        {
            ApiResult<WarehouseDetails> result = await WmsTopologyApiClient
                .TryReactivateWarehouseAsync(warehouse.Id);

            if (result.IsFailure)
            {
                Snackbar.Add(result.Error?.Message ?? Localizer["Warehouse.ReactivateError"], Severity.Error);

                return;
            }

            Snackbar.Add(Localizer["Warehouse.Reactivated"], Severity.Success);

            await ReloadAsync();
        }
        catch (Exception exception)
        {
            Snackbar.Add(exception.Message, Severity.Error);
        }
    }

    private void OnZonesRequested(WarehouseDetails warehouse)
    {
        NavigateToZones(warehouse.Id);
    }

    private void OnLocationsRequested(WarehouseDetails warehouse)
    {
        NavigateToLocations(warehouse.Id);
    }

    private void NavigateToZones(Guid warehouseId)
    {
        NavigationManager.NavigateTo($"/wms/topology/zones?warehouseId={warehouseId}");
    }

    private void NavigateToLocations(Guid warehouseId)
    {
        NavigationManager.NavigateTo($"/wms/topology/locations?warehouseId={warehouseId}");
    }

    private static GridData<WarehouseDetails> EmptyGridData()
    {
        return new GridData<WarehouseDetails>
        {
            Items = [],
            TotalItems = 0
        };
    }
}
