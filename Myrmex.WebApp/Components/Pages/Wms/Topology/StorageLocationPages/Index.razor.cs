using Microsoft.AspNetCore.Components;
using MudBlazor;
using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Topology;
using Myrmex.WebApp.Wms.Api;
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
    private List<StorageLocationTypeDetails> _storageLocationTypes = [];
    private List<StorageLocationStatusDetails> _storageLocationStatuses = [];
    private StorageLocationGrid? _storageLocationGrid;

    private Guid? _selectedWarehouseId;
    private Guid? _selectedZoneId;
    private Guid? _selectedStorageLocationTypeId;
    private Guid? _selectedStorageLocationStatusId;

    private bool _isLoadingWarehouses;
    private bool _isLoadingZones;
    private bool _isLoadingLookups;
    private string? _errorMessage;
    private string? _searchText;
    private bool _includeInactive;

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
        }

        await ResetAndReloadStorageLocationsAsync();
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

    private Task ReloadAsync()
    {
        return _storageLocationGrid?.ReloadServerDataAsync() ?? Task.CompletedTask;
    }

    private async Task OnWarehouseChanged(Guid? value)
    {
        _selectedWarehouseId = value;
        _selectedZoneId = null;
        _zones = [];

        UpdateUrl();

        if (_selectedWarehouseId is not null)
        {
            await LoadZonesAsync();
        }

        await ResetAndReloadStorageLocationsAsync();
    }

    private async Task OnZoneChanged(Guid? value)
    {
        _selectedZoneId = value;

        UpdateUrl();

        await ResetAndReloadStorageLocationsAsync();
    }

    private async Task OnSearchTextChanged(string? value)
    {
        _searchText = value;

        await ResetAndReloadStorageLocationsAsync();
    }

    private async Task OnStorageLocationTypeChanged(Guid? value)
    {
        _selectedStorageLocationTypeId = value;
        await ResetAndReloadStorageLocationsAsync();
    }

    private async Task OnStorageLocationStatusChanged(Guid? value)
    {
        _selectedStorageLocationStatusId = value;
        await ResetAndReloadStorageLocationsAsync();
    }

    private async Task OnIncludeInactiveChanged(bool value)
    {
        _includeInactive = value;

        await ResetAndReloadStorageLocationsAsync();
    }

    private async Task LoadWarehousesAsync()
    {
        _isLoadingWarehouses = true;
        _errorMessage = null;

        try
        {
            ListWarehousesRequest request = new()
            {
                Skip = 0,
                Take = 100,
                SortBy = WarehouseSortBy.Name,
                SortDescending = false,
                IncludeInactive = false
            };

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
            ListZonesRequest request = new()
            {
                WarehouseId = _selectedWarehouseId,
                Skip = 0,
                // The Zone selector remains a documented first-page preload for this feature.
                Take = 100,
                SortBy = ZoneSortBy.Code,
                SortDescending = false,
                IncludeInactive = false
            };

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

    private async Task<GridData<StorageLocationDetails>> LoadStorageLocationsAsync(
        StorageLocationGridRequest gridRequest,
        CancellationToken cancellationToken)
    {
        if (_selectedWarehouseId is null)
        {
            return EmptyGridData();
        }

        _errorMessage = null;

        try
        {
            ListStorageLocationsRequest request = new()
            {
                WarehouseId = _selectedWarehouseId,
                ZoneId = _selectedZoneId,
                StorageLocationTypeId = _selectedStorageLocationTypeId,
                StorageLocationStatusId = _selectedStorageLocationStatusId,
                Skip = gridRequest.Skip,
                Take = gridRequest.Take,
                SearchText = _searchText,
                SortBy = gridRequest.SortBy,
                SortDescending = gridRequest.SortDescending,
                IncludeInactive = _includeInactive
            };

            ListResult<StorageLocationDetails> result = _selectedZoneId is not null
                ? await WmsTopologyApiClient.ListStorageLocationsByZoneAsync(
                    _selectedZoneId.Value,
                    request,
                    cancellationToken)
                : await WmsTopologyApiClient.ListStorageLocationsByWarehouseAsync(
                    _selectedWarehouseId.Value,
                    request,
                    cancellationToken);

            return new GridData<StorageLocationDetails>
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
            return EmptyGridData();
        }
    }

    private Task ResetAndReloadStorageLocationsAsync()
    {
        return _storageLocationGrid?.ResetAndReloadServerDataAsync() ?? Task.CompletedTask;
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
            Snackbar.Add(Localizer["Common.SelectWarehouseFirst"], Severity.Warning);
            return;
        }

        if (_selectedZoneId is null)
        {
            Snackbar.Add(Localizer["Common.SelectZoneFirst"], Severity.Warning);
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
            Localizer["StorageLocation.CreateTitle"],
            parameters,
            options);

        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        Snackbar.Add(Localizer["StorageLocation.Created"], Severity.Success);

        await ReloadAsync();
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
            Localizer["StorageLocation.EditTitle"],
            parameters,
            options);

        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        Snackbar.Add(Localizer["StorageLocation.Updated"], Severity.Success);

        await ReloadAsync();
    }

    private async Task DeactivateStorageLocationAsync(StorageLocationDetails storageLocation)
    {
        try
        {
            ApiResult<StorageLocationDetails> result = await WmsTopologyApiClient
                .TryDeactivateStorageLocationAsync(storageLocation.Id);

            if (result.IsFailure)
            {
                Snackbar.Add(result.Error?.Message ?? Localizer["StorageLocation.DeactivateError"], Severity.Error);

                return;
            }

            Snackbar.Add(Localizer["StorageLocation.Deactivated"], Severity.Success);

            await ReloadAsync();
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
            ApiResult<StorageLocationDetails> result = await WmsTopologyApiClient
                .TryReactivateStorageLocationAsync(storageLocation.Id);

            if (result.IsFailure)
            {
                Snackbar.Add(result.Error?.Message ?? Localizer["StorageLocation.ReactivateError"], Severity.Error);

                return;
            }

            Snackbar.Add(Localizer["StorageLocation.Reactivated"], Severity.Success);

            await ReloadAsync();
        }
        catch (Exception exception)
        {
            Snackbar.Add(exception.Message, Severity.Error);
        }
    }

    private static GridData<StorageLocationDetails> EmptyGridData()
    {
        return new GridData<StorageLocationDetails>
        {
            Items = [],
            TotalItems = 0
        };
    }
}
