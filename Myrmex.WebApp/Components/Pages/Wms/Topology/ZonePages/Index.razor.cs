using Microsoft.AspNetCore.Components;
using MudBlazor;
using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Topology;
using Myrmex.WebApp.Wms.Api;
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
    private ZoneGrid? _zoneGrid;
    private Guid? _selectedWarehouseId;
    private bool _isLoadingWarehouses;
    private string? _errorMessage;
    private string? _searchText;
    private bool _includeInactive;

    protected override async Task OnInitializedAsync()
    {
        _selectedWarehouseId = WarehouseIdQuery;

        await LoadWarehousesAsync();
    }

    private Task ReloadAsync()
    {
        return _zoneGrid?.ReloadServerDataAsync() ?? Task.CompletedTask;
    }

    private async Task OnWarehouseChanged(Guid? value)
    {
        _selectedWarehouseId = value;

        UpdateUrl();
        await ResetAndReloadZonesAsync();
    }

    private async Task OnSearchTextChanged(string? value)
    {
        _searchText = value;

        await ResetAndReloadZonesAsync();
    }

    private async Task OnIncludeInactiveChanged(bool value)
    {
        _includeInactive = value;

        await ResetAndReloadZonesAsync();
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

    private async Task<GridData<ZoneDetails>> LoadZonesAsync(
        ZoneGridRequest gridRequest,
        CancellationToken cancellationToken)
    {
        if (_selectedWarehouseId is null)
        {
            return EmptyGridData();
        }

        _errorMessage = null;

        try
        {
            ListZonesRequest request = new()
            {
                WarehouseId = _selectedWarehouseId,
                Skip = gridRequest.Skip,
                Take = gridRequest.Take,
                SearchText = _searchText,
                SortBy = gridRequest.SortBy,
                SortDescending = gridRequest.SortDescending,
                IncludeInactive = _includeInactive
            };

            ListResult<ZoneDetails> result = await WmsTopologyApiClient
                .ListZonesAsync(_selectedWarehouseId.Value, request, cancellationToken);

            return new GridData<ZoneDetails>
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

    private Task ResetAndReloadZonesAsync()
    {
        return _zoneGrid?.ResetAndReloadServerDataAsync() ?? Task.CompletedTask;
    }

    private async Task CreateZoneAsync()
    {
        if (_selectedWarehouseId is null)
        {
            Snackbar.Add(Localizer["Common.SelectWarehouseFirst"], Severity.Warning);
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

        IDialogReference dialog = await DialogService.ShowAsync<ZoneEditDialog>(Localizer["Zone.CreateTitle"], parameters, options);

        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        Snackbar.Add(Localizer["Zone.Created"], Severity.Success);

        await ReloadAsync();
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

        IDialogReference dialog = await DialogService.ShowAsync<ZoneEditDialog>(Localizer["Zone.EditTitle"], parameters, options);

        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        Snackbar.Add(Localizer["Zone.Updated"], Severity.Success);

        await ReloadAsync();
    }

    private async Task DeactivateZoneAsync(ZoneDetails zone)
    {
        try
        {
            ApiResult<ZoneDetails> result = await WmsTopologyApiClient
                .TryDeactivateZoneAsync(zone.Id);

            if (result.IsFailure)
            {
                Snackbar.Add(result.Error?.Message ?? Localizer["Zone.DeactivateError"], Severity.Error);

                return;
            }

            Snackbar.Add(Localizer["Zone.Deactivated"], Severity.Success);

            await ReloadAsync();
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
                Snackbar.Add(result.Error?.Message ?? Localizer["Zone.ReactivateError"], Severity.Error);

                return;
            }

            Snackbar.Add(Localizer["Zone.Reactivated"], Severity.Success);

            await ReloadAsync();
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

    private static GridData<ZoneDetails> EmptyGridData()
    {
        return new GridData<ZoneDetails>
        {
            Items = [],
            TotalItems = 0
        };
    }
}

