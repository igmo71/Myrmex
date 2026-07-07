using Microsoft.AspNetCore.Components;
using MudBlazor;
using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Topology;
using Myrmex.Shared.Wms.Catalog;
using Myrmex.Shared.Wms.Inventory;
using Myrmex.WebApp.Wms.Catalog;
using Myrmex.WebApp.Wms.Inventory;
using Myrmex.WebApp.Wms.Topology;
using LookupStorageLocationsRequest = Myrmex.Shared.Wms.Topology.LookupStorageLocationsRequest;
using StorageLocationLookupItem = Myrmex.Shared.Wms.Topology.StorageLocationLookupItem;

namespace Myrmex.WebApp.Components.Pages.Wms.Inventory.InventoryTransferPages;

public partial class Index
{
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

    private InventoryTransferGrid? _inventoryTransferGrid;

    private List<WarehouseLookupItem> _warehouses = [];

    private Guid? _selectedWarehouseId;
    private string? _selectedStatus;
    private bool? _hasTransitLocation;
    private string? _transferCode;
    private DateTime? _createdFrom;
    private DateTime? _createdTo;
    private StockKeepingUnitLookupItem? _selectedStockKeepingUnit;
    private StorageLocationLookupItem? _selectedSourceStorageLocation;
    private StorageLocationLookupItem? _selectedDestinationStorageLocation;

    private bool _isLoadingWarehouses;
    private string? _errorMessage;
    private int _storageLocationSearchVersion;

    protected override async Task OnInitializedAsync()
    {
        await LoadWarehousesAsync();
    }

    private Task ReloadAsync()
    {
        return ReloadInventoryTransfersAsync();
    }

    private async Task OnWarehouseChanged(Guid? value)
    {
        _selectedWarehouseId = value;
        _selectedSourceStorageLocation = null;
        _selectedDestinationStorageLocation = null;
        _storageLocationSearchVersion++;

        await ResetAndReloadInventoryTransfersAsync();
    }

    private async Task OnStatusChanged(string? value)
    {
        _selectedStatus = value;
        await ResetAndReloadInventoryTransfersAsync();
    }

    private async Task OnHasTransitLocationChanged(bool? value)
    {
        _hasTransitLocation = value;
        await ResetAndReloadInventoryTransfersAsync();
    }

    private async Task OnTransferCodeChanged(string? value)
    {
        _transferCode = value;
        await ResetAndReloadInventoryTransfersAsync();
    }

    private async Task OnCreatedFromChanged(DateTime? value)
    {
        _createdFrom = value;
        await ResetAndReloadInventoryTransfersAsync();
    }

    private async Task OnCreatedToChanged(DateTime? value)
    {
        _createdTo = value;
        await ResetAndReloadInventoryTransfersAsync();
    }

    private async Task OnStockKeepingUnitChanged(StockKeepingUnitLookupItem? value)
    {
        _selectedStockKeepingUnit = value;
        await ResetAndReloadInventoryTransfersAsync();
    }

    private async Task OnSourceStorageLocationChanged(StorageLocationLookupItem? value)
    {
        if (value is not null &&
            (_selectedWarehouseId is null ||
             value.WarehouseId != _selectedWarehouseId.Value))
        {
            value = null;
        }

        _selectedSourceStorageLocation = value;
        await ResetAndReloadInventoryTransfersAsync();
    }

    private async Task OnDestinationStorageLocationChanged(StorageLocationLookupItem? value)
    {
        if (value is not null &&
            (_selectedWarehouseId is null ||
             value.WarehouseId != _selectedWarehouseId.Value))
        {
            value = null;
        }

        _selectedDestinationStorageLocation = value;
        await ResetAndReloadInventoryTransfersAsync();
    }

    private async Task<GridData<InventoryTransferListItem>> LoadInventoryTransfersAsync(
        InventoryTransferGridRequest gridRequest,
        CancellationToken cancellationToken)
    {
        _errorMessage = null;

        try
        {
            ListInventoryTransfersRequest request = new()
            {
                Skip = gridRequest.Skip,
                Take = gridRequest.Take,
                SortBy = gridRequest.SortBy,
                SortDescending = gridRequest.SortDescending,
                WarehouseId = _selectedWarehouseId,
                Status = _selectedStatus,
                CreatedFromUtc = ToStartOfDayUtc(_createdFrom),
                CreatedToUtc = ToEndOfDayUtc(_createdTo),
                TransferCode = _transferCode,
                SourceStorageLocationId = _selectedSourceStorageLocation?.Id,
                DestinationStorageLocationId = _selectedDestinationStorageLocation?.Id,
                StockKeepingUnitId = _selectedStockKeepingUnit?.Id,
                HasTransitLocation = _hasTransitLocation
            };

            ListResult<InventoryTransferListItem> result =
                await WmsInventoryApiClient.ListInventoryTransfersAsync(request, cancellationToken);

            return new GridData<InventoryTransferListItem>
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
            await InvokeAsync(StateHasChanged);

            return EmptyGridData();
        }
    }

    private Task ReloadInventoryTransfersAsync()
    {
        return _inventoryTransferGrid?.ReloadServerDataAsync()
            ?? Task.CompletedTask;
    }

    private Task ResetAndReloadInventoryTransfersAsync()
    {
        return _inventoryTransferGrid?.ResetAndReloadServerDataAsync()
            ?? Task.CompletedTask;
    }

    private async Task LoadWarehousesAsync()
    {
        _isLoadingWarehouses = true;
        _errorMessage = null;

        try
        {
            IReadOnlyList<WarehouseLookupItem> warehouses = await WmsTopologyApiClient
                .LookupWarehousesAsync(new LookupWarehousesRequest
                {
                    Take = AutocompleteTake,
                    SelectableOnly = true
                });

            _warehouses = warehouses.ToList();
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

    private async Task OpenCreateTransferAsync()
    {
        DialogOptions options = new()
        {
            CloseButton = false,
            MaxWidth = MaxWidth.Large,
            FullWidth = true
        };

        IDialogReference dialog = await DialogService
            .ShowAsync<CreateInventoryTransferDialog>(Localizer["InventoryTransfer.CreateTitle"], options);

        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        if (result.Data is InventoryTransferDetails transfer)
        {
            Snackbar.Add(Localizer["InventoryTransfer.Created"], Severity.Success);
            await ReloadInventoryTransfersAsync();
        }
    }

    private async Task OpenDetailsAsync(InventoryTransferListItem transfer)
    {
        _errorMessage = null;

        try
        {
            InventoryTransferDetails details = await WmsInventoryApiClient
                .GetInventoryTransferByIdAsync(transfer.Id);

            await OpenDetailsDialogAsync(details);
        }
        catch (Exception exception)
        {
            _errorMessage = exception.Message;
        }
    }

    private async Task OpenDetailsDialogAsync(InventoryTransferDetails transfer)
    {
        DialogParameters parameters = new()
        {
            [nameof(InventoryTransferDetailsDialog.Transfer)] = transfer,
            [nameof(InventoryTransferDetailsDialog.TransferChanged)] =
                EventCallback.Factory.Create<InventoryTransferDetails>(this, ReplaceTransfer)
        };

        DialogOptions options = new()
        {
            CloseButton = true,
            MaxWidth = MaxWidth.Large,
            FullWidth = true
        };

        await DialogService.ShowAsync<InventoryTransferDetailsDialog>(
            transfer.Code,
            parameters,
            options);
    }

    private async Task ReplaceTransfer(InventoryTransferDetails transfer)
    {
        await ReloadInventoryTransfersAsync();
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

    private static DateTimeOffset? ToStartOfDayUtc(DateTime? date)
    {
        return date is null
            ? null
            : new DateTimeOffset(DateTime.SpecifyKind(date.Value.Date, DateTimeKind.Local)).ToUniversalTime();
    }

    private static DateTimeOffset? ToEndOfDayUtc(DateTime? date)
    {
        return date is null
            ? null
            : new DateTimeOffset(DateTime.SpecifyKind(date.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Local))
                .ToUniversalTime();
    }

    private static GridData<InventoryTransferListItem> EmptyGridData()
    {
        return new GridData<InventoryTransferListItem>
        {
            Items = [],
            TotalItems = 0
        };
    }
}
