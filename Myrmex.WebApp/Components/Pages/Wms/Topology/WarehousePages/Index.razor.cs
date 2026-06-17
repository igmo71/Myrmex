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

    private List<WarehouseDetails> _warehouses = [];
    private bool _isLoading;
    private string? _errorMessage;
    private string? _searchText;
    private bool _includeInactive;

    protected override async Task OnInitializedAsync()
    {
        await LoadWarehousesAsync();
    }

    private async Task ReloadAsync()
    {
        await LoadWarehousesAsync();
    }

    private async Task OnSearchTextChanged(string? value)
    {
        _searchText = value;
        await LoadWarehousesAsync();
    }

    private async Task OnIncludeInactiveChanged(bool value)
    {
        _includeInactive = value;
        await LoadWarehousesAsync();
    }

    private async Task LoadWarehousesAsync()
    {
        _isLoading = true;
        _errorMessage = null;

        try
        {
            ListRequest request = new(
                Skip: 0,
                Take: 100,
                SearchText: _searchText,
                SortBy: "code",
                SortDescending: false,
                IncludeInactive: _includeInactive);

            ListResult<WarehouseDetails> result = await WmsTopologyApiClient.ListWarehousesAsync(request);

            _warehouses = result.Items.ToList();
        }
        catch (Exception exception)
        {
            _errorMessage = exception.Message;
            _warehouses = [];
        }
        finally
        {
            _isLoading = false;
        }
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
            .ShowAsync<WarehouseEditDialog>("Create warehouse", options);

        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        Snackbar.Add("Warehouse created.", Severity.Success);

        await LoadWarehousesAsync();
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
            .ShowAsync<WarehouseEditDialog>("Edit warehouse", parameters, options);

        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        Snackbar.Add("Warehouse updated.", Severity.Success);

        await LoadWarehousesAsync();
    }

    private async Task DeactivateWarehouseAsync(WarehouseDetails warehouse)
    {
        try
        {
            ApiResult<WarehouseDetails> result = await WmsTopologyApiClient
                .TryDeactivateWarehouseAsync(warehouse.Id);

            if (result.IsFailure)
            {
                Snackbar.Add(result.Error?.Message ?? "Warehouse deactivation failed.", Severity.Error);

                return;
            }

            Snackbar.Add("Warehouse deactivated.", Severity.Success);

            await LoadWarehousesAsync();
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
                Snackbar.Add(result.Error?.Message ?? "Warehouse reactivation failed.", Severity.Error);

                return;
            }

            Snackbar.Add("Warehouse reactivated.", Severity.Success);

            await LoadWarehousesAsync();
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
}
