using Microsoft.AspNetCore.Components;
using MudBlazor;
using Myrmex.WebApp.Wms.Api;
using Myrmex.WebApp.Wms.Catalog;
using Myrmex.WebApp.Wms.Inventory;
using Myrmex.WebApp.Wms.Topology;

namespace Myrmex.WebApp.Components.Pages.Wms.Inventory.InventoryBalancePages;

public partial class Index
{
    private const int LookupTake = 100;
    private const int BalanceTake = 100;

    [Inject]
    private WmsInventoryApiClient WmsInventoryApiClient { get; set; } = default!;

    [Inject]
    private WmsTopologyApiClient WmsTopologyApiClient { get; set; } = default!;

    [Inject]
    private WmsCatalogApiClient WmsCatalogApiClient { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    private List<InventoryBalanceDetails> _inventoryBalances = [];
    private List<WarehouseDetails> _warehouses = [];
    private List<StorageLocationDetails> _storageLocations = [];
    private List<StockKeepingUnitDetails> _skus = [];

    private Guid? _selectedWarehouseId;
    private Guid? _selectedStorageLocationId;
    private Guid? _selectedStockKeepingUnitId;

    private bool _isLoadingBalances;
    private bool _isLoadingWarehouses;
    private bool _isLoadingStorageLocations;
    private bool _isLoadingSkus;
    private string? _errorMessage;

    protected override async Task OnInitializedAsync()
    {
        await LoadWarehousesAsync();
        await LoadSkusAsync();
        await LoadInventoryBalancesAsync();
    }

    private async Task ReloadAsync()
    {
        await LoadInventoryBalancesAsync();
    }

    private async Task OnWarehouseChanged(Guid? value)
    {
        _selectedWarehouseId = value;
        _selectedStorageLocationId = null;
        _storageLocations = [];

        if (_selectedWarehouseId is not null)
        {
            await LoadStorageLocationsAsync();
        }

        await LoadInventoryBalancesAsync();
    }

    private async Task OnStorageLocationChanged(Guid? value)
    {
        _selectedStorageLocationId = value;
        await LoadInventoryBalancesAsync();
    }

    private async Task OnStockKeepingUnitChanged(Guid? value)
    {
        _selectedStockKeepingUnitId = value;
        await LoadInventoryBalancesAsync();
    }

    private async Task LoadWarehousesAsync()
    {
        _isLoadingWarehouses = true;
        _errorMessage = null;

        try
        {
            ListRequest request = new(
                Skip: 0,
                Take: LookupTake,
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

    private async Task LoadSkusAsync()
    {
        _isLoadingSkus = true;
        _errorMessage = null;

        try
        {
            ListRequest request = new(
                Skip: 0,
                Take: LookupTake,
                SortBy: "code",
                SortDescending: false,
                IncludeInactive: false);

            ListResult<StockKeepingUnitDetails> result = await WmsCatalogApiClient
                .ListStockKeepingUnitsAsync(request);

            _skus = result.Items.ToList();
        }
        catch (Exception exception)
        {
            _errorMessage = exception.Message;
            _skus = [];
        }
        finally
        {
            _isLoadingSkus = false;
        }
    }

    private async Task LoadStorageLocationsAsync()
    {
        if (_selectedWarehouseId is null)
        {
            _storageLocations = [];
            return;
        }

        _isLoadingStorageLocations = true;
        _errorMessage = null;

        try
        {
            ListRequest request = new(
                Skip: 0,
                Take: LookupTake,
                SortBy: "code",
                SortDescending: false,
                IncludeInactive: false);

            ListResult<StorageLocationDetails> result = await WmsTopologyApiClient
                .ListStorageLocationsByWarehouseAsync(_selectedWarehouseId.Value, request);

            _storageLocations = result.Items.ToList();

            if (_selectedStorageLocationId is not null &&
                _storageLocations.All(x => x.Id != _selectedStorageLocationId.Value))
            {
                _selectedStorageLocationId = null;
            }
        }
        catch (Exception exception)
        {
            _errorMessage = exception.Message;
            _storageLocations = [];
            _selectedStorageLocationId = null;
        }
        finally
        {
            _isLoadingStorageLocations = false;
        }
    }

    private async Task LoadInventoryBalancesAsync()
    {
        _isLoadingBalances = true;
        _errorMessage = null;

        try
        {
            ListInventoryBalancesRequest request = new(
                Skip: 0,
                Take: BalanceTake,
                SortBy: "id",
                SortDescending: false,
                StockKeepingUnitId: _selectedStockKeepingUnitId,
                StorageLocationId: _selectedStorageLocationId,
                WarehouseId: _selectedWarehouseId);

            ListResult<InventoryBalanceDetails> result = await WmsInventoryApiClient
                .ListInventoryBalancesAsync(request);

            _inventoryBalances = result.Items.ToList();
        }
        catch (Exception exception)
        {
            _errorMessage = exception.Message;
            _inventoryBalances = [];
        }
        finally
        {
            _isLoadingBalances = false;
        }
    }

    private async Task CreateInventoryBalanceAsync()
    {
        DialogOptions options = new()
        {
            CloseButton = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        IDialogReference dialog = await DialogService
            .ShowAsync<CreateInventoryBalanceDialog>("Create inventory balance", options);

        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        string message = result.Data is InventoryBalanceDetails createdBalance &&
                         !MatchesActiveFilters(createdBalance)
            ? "Inventory balance created. Active filters may hide the new balance."
            : "Inventory balance created.";

        Snackbar.Add(message, Severity.Success);

        await LoadInventoryBalancesAsync();
    }

    private async Task UpdateInventoryBalanceQuantityAsync(InventoryBalanceDetails inventoryBalance)
    {
        DialogParameters parameters = new()
        {
            [nameof(UpdateInventoryBalanceQuantityDialog.InventoryBalance)] = inventoryBalance
        };

        DialogOptions options = new()
        {
            CloseButton = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        IDialogReference dialog = await DialogService.ShowAsync<UpdateInventoryBalanceQuantityDialog>(
            "Update inventory balance quantity",
            parameters,
            options);

        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        Snackbar.Add("Inventory balance quantity updated.", Severity.Success);

        await LoadInventoryBalancesAsync();
    }

    private bool MatchesActiveFilters(InventoryBalanceDetails inventoryBalance)
    {
        if (_selectedWarehouseId is not null &&
            inventoryBalance.WarehouseId != _selectedWarehouseId.Value)
        {
            return false;
        }

        if (_selectedStorageLocationId is not null &&
            inventoryBalance.StorageLocationId != _selectedStorageLocationId.Value)
        {
            return false;
        }

        if (_selectedStockKeepingUnitId is not null &&
            inventoryBalance.StockKeepingUnitId != _selectedStockKeepingUnitId.Value)
        {
            return false;
        }

        return true;
    }
}
