using Microsoft.AspNetCore.Components;
using MudBlazor;
using Myrmex.WebApp.Wms.Topology;

namespace Myrmex.WebApp.Components.Pages.Wms.Topology.ZonePages;

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

    [Parameter]
    [SupplyParameterFromQuery(Name = "warehouseId")]
    public Guid? WarehouseIdQuery { get; set; }

    private List<WarehouseDetails> _warehouses = [];
    private List<ZoneDetails> _zones = [];

    private Guid? _selectedWarehouseId;
    private bool _isLoading;
    private bool _isLoadingWarehouses;
    private string? _errorMessage;
    private string? _searchText;
    private bool _includeInactive;

    protected override async Task OnInitializedAsync()
    {
        _selectedWarehouseId = WarehouseIdQuery;

        await LoadWarehousesAsync();

        if (_selectedWarehouseId is not null)
        {
            await LoadZonesAsync();
        }
    }

    private async Task ReloadAsync()
    {
        if (_selectedWarehouseId is null)
        {
            _zones = [];
            return;
        }

        await LoadZonesAsync();
    }

    private async Task OnWarehouseChanged(Guid? value)
    {
        _selectedWarehouseId = value;
        _zones = [];

        UpdateUrl();

        if (_selectedWarehouseId is not null)
        {
            await LoadZonesAsync();
        }
    }

    private async Task OnSearchTextChanged(string? value)
    {
        _searchText = value;

        if (_selectedWarehouseId is not null)
        {
            await LoadZonesAsync();
        }
    }

    private async Task OnIncludeInactiveChanged(bool value)
    {
        _includeInactive = value;

        if (_selectedWarehouseId is not null)
        {
            await LoadZonesAsync();
        }
    }

    private async Task LoadWarehousesAsync()
    {
        _isLoadingWarehouses = true;
        _errorMessage = null;

        try
        {
            ListRequest request = new(
                Skip: 0,
                Take: 100,
                SearchText: null,
                SortBy: "code",
                SortDescending: false,
                IncludeInactive: false);

            ListResult<WarehouseDetails> result = await WmsTopologyApiClient
                .ListWarehousesAsync(request);

            _warehouses = result.Items.ToList();
        }
        catch (Exception exception)
        {
            _errorMessage = exception.Message;
            _warehouses = [];
        }
        finally
        {
            _isLoadingWarehouses = false;
        }
    }

    private async Task LoadZonesAsync()
    {
        if (_selectedWarehouseId is null)
        {
            _zones = [];
            return;
        }

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

            ListResult<ZoneDetails> result = await WmsTopologyApiClient
                .ListZonesAsync(_selectedWarehouseId.Value, request);

            _zones = result.Items.ToList();
        }
        catch (Exception exception)
        {
            _errorMessage = exception.Message;
            _zones = [];
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task CreateZoneAsync()
    {
        if (_selectedWarehouseId is null)
        {
            Snackbar.Add("Select a warehouse first.", Severity.Warning);
            return;
        }

        DialogParameters parameters = new()
        {
            [nameof(ZoneEditDialog.WarehouseId)] = _selectedWarehouseId.Value
        };

        DialogOptions options = new()
        {
            CloseButton = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        IDialogReference dialog = await DialogService.ShowAsync<ZoneEditDialog>("Create zone", parameters, options);

        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        Snackbar.Add("Zone created.", Severity.Success);

        await LoadZonesAsync();
    }

    private async Task EditZoneAsync(ZoneDetails zone)
    {
        DialogParameters parameters = new()
        {
            [nameof(ZoneEditDialog.WarehouseId)] = zone.WarehouseId,
            [nameof(ZoneEditDialog.Zone)] = zone
        };

        DialogOptions options = new()
        {
            CloseButton = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        IDialogReference dialog = await DialogService.ShowAsync<ZoneEditDialog>("Edit zone", parameters, options);

        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        Snackbar.Add("Zone updated.", Severity.Success);

        await LoadZonesAsync();
    }

    private async Task DeactivateZoneAsync(ZoneDetails zone)
    {
        try
        {
            ApiResult<ZoneDetails> result = await WmsTopologyApiClient
                .TryDeactivateZoneAsync(zone.Id);

            if (result.IsFailure)
            {
                Snackbar.Add(result.Error?.Message ?? "Zone deactivation failed.", Severity.Error);

                return;
            }

            Snackbar.Add("Zone deactivated.", Severity.Success);

            await LoadZonesAsync();
        }
        catch (Exception exception)
        {
            Snackbar.Add(exception.Message, Severity.Error);
        }
    }

    private async Task ReactivateZoneAsync(ZoneDetails zone)
    {
        try
        {
            ApiResult<ZoneDetails> result = await WmsTopologyApiClient
                .TryReactivateZoneAsync(zone.Id);

            if (result.IsFailure)
            {
                Snackbar.Add(result.Error?.Message ?? "Zone reactivation failed.", Severity.Error);

                return;
            }

            Snackbar.Add("Zone reactivated.", Severity.Success);

            await LoadZonesAsync();
        }
        catch (Exception exception)
        {
            Snackbar.Add(exception.Message, Severity.Error);
        }
    }

    private void UpdateUrl()
    {
        if (_selectedWarehouseId is null)
        {
            NavigationManager.NavigateTo("/wms/topology/zones", replace: true);
            return;
        }

        NavigationManager.NavigateTo(
            $"/wms/topology/zones?warehouseId={_selectedWarehouseId.Value}",
            replace: true);
    }

    private void NavigateToLocations(ZoneDetails zone)
    {
        NavigationManager.NavigateTo(
            $"/wms/topology/locations?warehouseId={zone.WarehouseId}&zoneId={zone.Id}");
    }
}

