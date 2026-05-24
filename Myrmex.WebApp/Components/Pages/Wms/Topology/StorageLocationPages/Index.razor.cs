using Microsoft.AspNetCore.Components;
using MudBlazor;
using Myrmex.WebApp.Wms.Topology;

namespace Myrmex.WebApp.Components.Pages.Wms.Topology.StorageLocationPages;

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

    [Parameter]
    [SupplyParameterFromQuery(Name = "zoneId")]
    public Guid? ZoneIdQuery { get; set; }

    private List<WarehouseDetails> _warehouses = [];
    private List<ZoneDetails> _zones = [];
    private List<StorageLocationDetails> _storageLocations = [];
    private List<StorageLocationTypeDetails> _storageLocationTypes = [];
    private List<StorageLocationStatusDetails> _storageLocationStatuses = [];

    private Guid? _selectedWarehouseId;
    private Guid? _selectedZoneId;
    private Guid? _selectedStorageLocationTypeId;
    private Guid? _selectedStorageLocationStatusId;

    private bool _isLoading;
    private bool _isLoadingWarehouses;
    private bool _isLoadingZones;
    private bool _isLoadingLookups;
    private string? _errorMessage;
    private string? _searchText;
    private bool _includeInactive;

    private IEnumerable<StorageLocationDetails> FilteredStorageLocations =>
        _storageLocations
            .Where(x => _selectedStorageLocationTypeId is null ||
                        x.StorageLocationTypeId == _selectedStorageLocationTypeId.Value)
            .Where(x => _selectedStorageLocationStatusId is null ||
                        x.StorageLocationStatusId == _selectedStorageLocationStatusId.Value);

    protected override async Task OnInitializedAsync()
    {
        _selectedWarehouseId = WarehouseIdQuery;
        _selectedZoneId = ZoneIdQuery;

        await LoadLookupsAsync();
        await LoadWarehousesAsync();

        if (_selectedZoneId is not null && _selectedWarehouseId is null)
        {
            await ResolveWarehouseFromZoneAsync(_selectedZoneId.Value);
        }

        if (_selectedWarehouseId is not null)
        {
            await LoadZonesAsync();
            await LoadStorageLocationsAsync();
        }
    }

    private async Task ResolveWarehouseFromZoneAsync(Guid zoneId)
    {
        try
        {
            ZoneDetails zone = await WmsTopologyApiClient.GetZoneByIdAsync(zoneId);
            _selectedWarehouseId = zone.WarehouseId;
        }
        catch (Exception exception)
        {
            _errorMessage = exception.Message;
            _selectedZoneId = null;
        }
    }

    private async Task ReloadAsync()
    {
        await LoadStorageLocationsAsync();
    }

    private async Task OnWarehouseChanged(Guid? value)
    {
        _selectedWarehouseId = value;
        _selectedZoneId = null;
        _zones = [];
        _storageLocations = [];

        UpdateUrl();

        if (_selectedWarehouseId is not null)
        {
            await LoadZonesAsync();
            await LoadStorageLocationsAsync();
        }
    }

    private async Task OnZoneChanged(Guid? value)
    {
        _selectedZoneId = value;

        UpdateUrl();

        if (_selectedWarehouseId is not null)
        {
            await LoadStorageLocationsAsync();
        }
    }

    private async Task OnSearchTextChanged(string? value)
    {
        _searchText = value;

        if (_selectedWarehouseId is not null)
        {
            await LoadStorageLocationsAsync();
        }
    }

    private void OnStorageLocationTypeChanged(Guid? value)
    {
        _selectedStorageLocationTypeId = value;
    }

    private void OnStorageLocationStatusChanged(Guid? value)
    {
        _selectedStorageLocationStatusId = value;
    }

    private async Task OnIncludeInactiveChanged(bool value)
    {
        _includeInactive = value;

        if (_selectedWarehouseId is not null)
        {
            await LoadStorageLocationsAsync();
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

        _isLoadingZones = true;
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

            ListResult<ZoneDetails> result = await WmsTopologyApiClient
                .ListZonesAsync(_selectedWarehouseId.Value, request);

            _zones = result.Items.ToList();

            if (_selectedZoneId is not null &&
                _zones.All(x => x.Id != _selectedZoneId.Value))
            {
                _selectedZoneId = null;
                UpdateUrl();
            }
        }
        catch (Exception exception)
        {
            _errorMessage = exception.Message;
            _zones = [];
        }
        finally
        {
            _isLoadingZones = false;
        }
    }

    private async Task LoadLookupsAsync()
    {
        _isLoadingLookups = true;
        _errorMessage = null;

        try
        {
            IReadOnlyList<StorageLocationTypeDetails> types = await WmsTopologyApiClient
                .ListStorageLocationTypesAsync();

            IReadOnlyList<StorageLocationStatusDetails> statuses = await WmsTopologyApiClient
                .ListStorageLocationStatusesAsync();

            _storageLocationTypes = types.ToList();
            _storageLocationStatuses = statuses.ToList();
        }
        catch (Exception exception)
        {
            _errorMessage = exception.Message;
            _storageLocationTypes = [];
            _storageLocationStatuses = [];
        }
        finally
        {
            _isLoadingLookups = false;
        }
    }

    private async Task LoadStorageLocationsAsync()
    {
        if (_selectedWarehouseId is null)
        {
            _storageLocations = [];
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

            ListResult<StorageLocationDetails> result = _selectedZoneId is not null
                ? await WmsTopologyApiClient.ListStorageLocationsByZoneAsync(_selectedZoneId.Value, request)
                : await WmsTopologyApiClient.ListStorageLocationsByWarehouseAsync(_selectedWarehouseId.Value, request);

            _storageLocations = result.Items.ToList();
        }
        catch (Exception exception)
        {
            _errorMessage = exception.Message;
            _storageLocations = [];
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void UpdateUrl()
    {
        if (_selectedWarehouseId is null)
        {
            NavigationManager.NavigateTo("/wms/topology/locations", replace: true);
            return;
        }

        if (_selectedZoneId is null)
        {
            NavigationManager.NavigateTo(
                $"/wms/topology/locations?warehouseId={_selectedWarehouseId.Value}",
                replace: true);
            return;
        }

        NavigationManager.NavigateTo(
            $"/wms/topology/locations?warehouseId={_selectedWarehouseId.Value}&zoneId={_selectedZoneId.Value}",
            replace: true);
    }

    private async Task CreateStorageLocationAsync()
    {
        if (_selectedWarehouseId is null)
        {
            Snackbar.Add("Select a warehouse first.", Severity.Warning);
            return;
        }

        if (_selectedZoneId is null)
        {
            Snackbar.Add("Select a zone first.", Severity.Warning);
            return;
        }

        DialogParameters parameters = new()
        {
            [nameof(StorageLocationEditDialog.WarehouseId)] = _selectedWarehouseId.Value,
            [nameof(StorageLocationEditDialog.ZoneId)] = _selectedZoneId.Value
        };

        DialogOptions options = new()
        {
            CloseButton = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        IDialogReference dialog = await DialogService.ShowAsync<StorageLocationEditDialog>(
            "Create storage location",
            parameters,
            options);

        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        Snackbar.Add("Storage location created.", Severity.Success);

        await LoadStorageLocationsAsync();
    }

    private async Task EditStorageLocationAsync(StorageLocationDetails storageLocation)
    {
        DialogParameters parameters = new()
        {
            [nameof(StorageLocationEditDialog.WarehouseId)] = storageLocation.WarehouseId,
            [nameof(StorageLocationEditDialog.ZoneId)] = storageLocation.ZoneId,
            [nameof(StorageLocationEditDialog.StorageLocation)] = storageLocation
        };

        DialogOptions options = new()
        {
            CloseButton = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        IDialogReference dialog = await DialogService.ShowAsync<StorageLocationEditDialog>(
            "Edit storage location",
            parameters,
            options);

        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        Snackbar.Add("Storage location updated.", Severity.Success);

        await LoadStorageLocationsAsync();
    }

    private async Task DeactivateStorageLocationAsync(StorageLocationDetails storageLocation)
    {
        try
        {
            await WmsTopologyApiClient.DeactivateStorageLocationAsync(storageLocation.Id);

            Snackbar.Add("Storage location deactivated.", Severity.Success);

            await LoadStorageLocationsAsync();
        }
        catch (Exception exception)
        {
            Snackbar.Add(exception.Message, Severity.Error);
        }
    }

    private async Task ReactivateStorageLocationAsync(StorageLocationDetails storageLocation)
    {
        try
        {
            await WmsTopologyApiClient.ReactivateStorageLocationAsync(storageLocation.Id);

            Snackbar.Add("Storage location reactivated.", Severity.Success);

            await LoadStorageLocationsAsync();
        }
        catch (Exception exception)
        {
            Snackbar.Add(exception.Message, Severity.Error);
        }
    }
}
