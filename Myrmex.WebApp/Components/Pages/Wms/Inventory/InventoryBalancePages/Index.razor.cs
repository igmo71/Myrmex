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

    private InventoryBalanceGrid? _inventoryBalanceGrid;

    private List<WarehouseDetails> _warehouses = [];
    private List<StorageLocationDetails> _storageLocations = [];
    private List<StockKeepingUnitDetails> _skus = [];

    private Guid? _selectedWarehouseId;
    private Guid? _selectedStorageLocationId;
    private Guid? _selectedStockKeepingUnitId;

    private bool _isLoadingWarehouses;
    private bool _isLoadingStorageLocations;
    private bool _isLoadingSkus;
    private string? _errorMessage;

    protected override async Task OnInitializedAsync()
    {
        await Task.WhenAll(
            LoadWarehousesAsync(),
            LoadSkusAsync());
    }

    private Task ReloadAsync()
    {
        return ReloadInventoryBalancesAsync();
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

        await ResetAndReloadInventoryBalancesAsync();
    }

    private async Task OnStorageLocationChanged(Guid? value)
    {
        _selectedStorageLocationId = value;
        await ResetAndReloadInventoryBalancesAsync();
    }

    private async Task OnStockKeepingUnitChanged(Guid? value)
    {
        _selectedStockKeepingUnitId = value;
        await ResetAndReloadInventoryBalancesAsync();
    }

    private async Task<GridData<InventoryBalanceDetails>> LoadInventoryBalancesAsync(
        InventoryBalanceGridRequest gridRequest,
        CancellationToken cancellationToken)
    {
        _errorMessage = null;

        try
        {
            ListInventoryBalancesRequest request = new(
            Skip: gridRequest.Skip,
            Take: gridRequest.Take,
            SortBy: gridRequest.SortBy,
            SortDescending: gridRequest.SortDescending,
            StockKeepingUnitId: _selectedStockKeepingUnitId,
            StorageLocationId: _selectedStorageLocationId,
            WarehouseId: _selectedWarehouseId);

            ListResult<InventoryBalanceDetails> result =
                await WmsInventoryApiClient.ListInventoryBalancesAsync(request, cancellationToken);

            return new GridData<InventoryBalanceDetails>
            {
                Items = result.Items,
                TotalItems = result.TotalCount
            };
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _errorMessage = exception.Message;

            return new GridData<InventoryBalanceDetails>
            {
                Items = [],
                TotalItems = 0
            };
        }
    }

    private Task ReloadInventoryBalancesAsync()
    {
        return _inventoryBalanceGrid?.ReloadServerDataAsync()
            ?? Task.CompletedTask;
    }
    private Task ResetAndReloadInventoryBalancesAsync()
    {
        return _inventoryBalanceGrid?.ResetAndReloadServerDataAsync()
            ?? Task.CompletedTask;
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

        await ReloadInventoryBalancesAsync();
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

        await ReloadInventoryBalancesAsync();
    }

    private bool MatchesActiveFilters(InventoryBalanceDetails inventoryBalance)
    {
        if (_selectedWarehouseId is not null &&
            inventoryBalance.Location.Warehouse.Id != _selectedWarehouseId.Value)
        {
            return false;
        }

        if (_selectedStorageLocationId is not null &&
            inventoryBalance.Location.Id != _selectedStorageLocationId.Value)
        {
            return false;
        }

        if (_selectedStockKeepingUnitId is not null &&
            inventoryBalance.Sku.Id != _selectedStockKeepingUnitId.Value)
        {
            return false;
        }

        return true;
    }
}
