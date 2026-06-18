using Microsoft.AspNetCore.Components;
using MudBlazor;
using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Catalog;
using Myrmex.Shared.Wms.Inventory;
using Myrmex.Shared.Wms.Topology;
using Myrmex.WebApp.Wms.Catalog;
using Myrmex.WebApp.Wms.Inventory;
using Myrmex.WebApp.Wms.Topology;

namespace Myrmex.WebApp.Components.Pages.Wms.Inventory.InventoryBalancePages;

public partial class Index
{
    private const int LookupTake = 100;
    private const int AutocompleteTake = 20;

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

    private Guid? _selectedWarehouseId;
    private StorageLocationLookupItem? _selectedStorageLocation;
    private StockKeepingUnitLookupItem? _selectedStockKeepingUnit;

    private bool _isLoadingWarehouses;
    private string? _errorMessage;
    private int _storageLocationSearchVersion;

    protected override async Task OnInitializedAsync()
    {
        await LoadWarehousesAsync();
    }

    private Task ReloadAsync()
    {
        return ReloadInventoryBalancesAsync();
    }

    private async Task OnWarehouseChanged(Guid? value)
    {
        _selectedWarehouseId = value;
        _selectedStorageLocation = null;
        _storageLocationSearchVersion++;

        await ResetAndReloadInventoryBalancesAsync();
    }

    private async Task OnStorageLocationChanged(StorageLocationLookupItem? value)
    {
        if (value is not null &&
            (_selectedWarehouseId is null ||
             value.WarehouseId != _selectedWarehouseId.Value))
        {
            value = null;
        }

        _selectedStorageLocation = value;
        await ResetAndReloadInventoryBalancesAsync();
    }

    private async Task OnStockKeepingUnitChanged(StockKeepingUnitLookupItem? value)
    {
        _selectedStockKeepingUnit = value;
        await ResetAndReloadInventoryBalancesAsync();
    }

    private async Task<GridData<InventoryBalanceDetails>> LoadInventoryBalancesAsync(
        InventoryBalanceGridRequest gridRequest,
        CancellationToken cancellationToken)
    {
        _errorMessage = null;

        try
        {
            ListInventoryBalancesRequest request = new()
            {
                Skip = gridRequest.Skip,
                Take = gridRequest.Take,
                SortBy = gridRequest.SortBy,
                SortDescending = gridRequest.SortDescending,
                StockKeepingUnitId = _selectedStockKeepingUnit?.Id,
                StorageLocationId = _selectedStorageLocation?.Id,
                WarehouseId = _selectedWarehouseId
            };

            ListResult<InventoryBalanceDetails> result =
                await WmsInventoryApiClient.ListInventoryBalancesAsync(request, cancellationToken);

            return new GridData<InventoryBalanceDetails>
            {
                Items = result.Items,
                TotalItems = result.TotalCount
            };
        }
        catch (Exception exception)
            when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
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

    private async Task AdjustInventoryBalanceAsync(InventoryBalanceDetails inventoryBalance)
    {
        DialogParameters parameters = new()
        {
            [nameof(AdjustInventoryBalanceDialog.InventoryBalance)] = inventoryBalance
        };

        DialogOptions options = new()
        {
            CloseButton = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        IDialogReference dialog = await DialogService.ShowAsync<AdjustInventoryBalanceDialog>(
            "Adjust inventory balance",
            parameters,
            options);

        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        Snackbar.Add("Inventory balance adjusted.", Severity.Success);

        await ReloadInventoryBalancesAsync();
    }

    private bool MatchesActiveFilters(InventoryBalanceDetails inventoryBalance)
    {
        if (_selectedWarehouseId is not null &&
            inventoryBalance.StorageLocation.Warehouse.Id != _selectedWarehouseId.Value)
        {
            return false;
        }

        if (_selectedStorageLocation is not null &&
            inventoryBalance.StorageLocation.Id != _selectedStorageLocation.Id)
        {
            return false;
        }

        if (_selectedStockKeepingUnit is not null &&
            inventoryBalance.Sku.Id != _selectedStockKeepingUnit.Id)
        {
            return false;
        }

        return true;
    }

    private async Task<IEnumerable<StockKeepingUnitLookupItem>> SearchStockKeepingUnitsAsync(
        string value,
        CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyList<StockKeepingUnitLookupItem> items = await WmsCatalogApiClient
                .LookupStockKeepingUnitsAsync(
                    new LookupStockKeepingUnitsRequest
                    {
                        SearchText = value,
                        Take = AutocompleteTake,
                        SelectableOnly = false
                    },
                    cancellationToken);

            return items;
        }
        catch (Exception exception)
            when (exception is OperationCanceledException && cancellationToken.IsCancellationRequested)
        {
            return [];
        }
        catch (Exception exception)
        {
            _errorMessage = exception.Message;
            return [];
        }
    }

    private async Task<IEnumerable<StorageLocationLookupItem>> SearchStorageLocationsAsync(
        string value,
        CancellationToken cancellationToken)
    {
        if (_selectedWarehouseId is not Guid warehouseId)
        {
            return [];
        }

        int searchVersion = _storageLocationSearchVersion;

        try
        {
            IReadOnlyList<StorageLocationLookupItem> items = await WmsTopologyApiClient
                .LookupStorageLocationsAsync(
                    warehouseId,
                    new LookupStorageLocationsRequest
                    {
                        SearchText = value,
                        Take = AutocompleteTake,
                        SelectableOnly = false
                    },
                    cancellationToken);

            return searchVersion == _storageLocationSearchVersion &&
                   _selectedWarehouseId == warehouseId
                ? items
                : [];
        }
        catch (Exception exception)
            when (exception is OperationCanceledException && cancellationToken.IsCancellationRequested)
        {
            return [];
        }
        catch (Exception exception)
        {
            _errorMessage = exception.Message;
            return [];
        }
    }
}
