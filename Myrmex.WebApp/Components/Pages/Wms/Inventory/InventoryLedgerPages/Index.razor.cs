using Microsoft.AspNetCore.Components;
using MudBlazor;
using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Catalog;
using Myrmex.Shared.Wms.Inventory;
using Myrmex.Shared.Wms.Topology;
using Myrmex.WebApp.Wms.Catalog;
using Myrmex.WebApp.Wms.Inventory;
using Myrmex.WebApp.Wms.Topology;
using System.Globalization;

namespace Myrmex.WebApp.Components.Pages.Wms.Inventory.InventoryLedgerPages;

public partial class Index
{
    private const int LookupTake = 100;
    private const int AutocompleteTake = 20;
    private const string LedgerRoute = "/wms/inventory/ledger";

    [Inject]
    private WmsInventoryApiClient WmsInventoryApiClient { get; set; } = default!;

    [Inject]
    private WmsTopologyApiClient WmsTopologyApiClient { get; set; } = default!;

    [Inject]
    private WmsCatalogApiClient WmsCatalogApiClient { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Parameter]
    [SupplyParameterFromQuery(Name = "stockKeepingUnitId")]
    public Guid? RoutedStockKeepingUnitId { get; set; }

    [Parameter]
    [SupplyParameterFromQuery(Name = "warehouseId")]
    public Guid? RoutedWarehouseId { get; set; }

    [Parameter]
    [SupplyParameterFromQuery(Name = "storageLocationId")]
    public Guid? RoutedStorageLocationId { get; set; }

    private InventoryLedgerGrid? _inventoryLedgerGrid;

    private List<WarehouseDetails> _warehouses = [];

    private Guid? _selectedWarehouseId;
    private StorageLocationLookupItem? _selectedStorageLocation;
    private StockKeepingUnitLookupItem? _selectedStockKeepingUnit;
    private string _selectedTransactionType = string.Empty;
    private string? _occurredFromUtcText;
    private string? _occurredToUtcText;

    private bool _isInitializing = true;
    private bool _isLoadingWarehouses;
    private bool _isRouteStateBlockingLedgerRequests;
    private string? _errorMessage;
    private string? _routeValidationMessage;
    private int _storageLocationSearchVersion;

    private bool CanLoadLedgerGrid =>
        !_isInitializing &&
        !_isRouteStateBlockingLedgerRequests;

    private bool HasRoutedFilters =>
        RoutedStockKeepingUnitId.HasValue ||
        RoutedWarehouseId.HasValue ||
        RoutedStorageLocationId.HasValue;

    protected override async Task OnInitializedAsync()
    {
        _isInitializing = true;

        try
        {
            await LoadWarehousesAsync();

            if (HasRoutedFilters)
            {
                await HydrateRoutedFiltersAsync();
            }
        }
        finally
        {
            _isInitializing = false;
        }
    }

    private Task ReloadAsync()
    {
        return ReloadInventoryLedgerAsync();
    }

    private async Task OnWarehouseChanged(Guid? value)
    {
        _selectedWarehouseId = value;
        _selectedStorageLocation = null;
        _storageLocationSearchVersion++;
        ClearRouteBlockingState();

        await ResetAndReloadInventoryLedgerAsync();
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
        ClearRouteBlockingState();

        await ResetAndReloadInventoryLedgerAsync();
    }

    private async Task OnStockKeepingUnitChanged(StockKeepingUnitLookupItem? value)
    {
        _selectedStockKeepingUnit = value;
        //ClearRouteBlockingState();

        await ResetAndReloadInventoryLedgerAsync();
    }

    private async Task OnTransactionTypeChanged(string value)
    {
        _selectedTransactionType = value;
        await ResetAndReloadInventoryLedgerAsync();
    }

    private async Task OnOccurredFromUtcTextChanged(string? value)
    {
        _occurredFromUtcText = value;
        await ResetAndReloadInventoryLedgerAsync();
    }

    private async Task OnOccurredToUtcTextChanged(string? value)
    {
        _occurredToUtcText = value;
        await ResetAndReloadInventoryLedgerAsync();
    }

    private async Task ClearFiltersAsync()
    {
        _selectedWarehouseId = null;
        _selectedStorageLocation = null;
        _selectedStockKeepingUnit = null;
        _selectedTransactionType = string.Empty;
        _occurredFromUtcText = null;
        _occurredToUtcText = null;
        _storageLocationSearchVersion++;
        ClearRouteBlockingState();
        NavigationManager.NavigateTo(LedgerRoute, replace: true);

        await ResetAndReloadInventoryLedgerAsync();
    }

    private async Task<GridData<InventoryLedgerEntryDetails>> LoadInventoryLedgerEntriesAsync(
        InventoryLedgerGridRequest gridRequest,
        CancellationToken cancellationToken)
    {
        if (!CanLoadLedgerGrid)
        {
            return EmptyGridData();
        }

        _errorMessage = null;

        if (!TryCreateOccurrenceRange(out DateTimeOffset? occurredFromUtc, out DateTimeOffset? occurredToUtc, out string? validationMessage))
        {
            _errorMessage = validationMessage;
            return EmptyGridData();
        }

        try
        {
            ListInventoryLedgerEntriesRequest request = new()
            {
                Skip = gridRequest.Skip,
                Take = gridRequest.Take,
                SortBy = string.IsNullOrWhiteSpace(gridRequest.SortBy)
                    ? null
                    : gridRequest.SortBy,
                SortDescending = string.IsNullOrWhiteSpace(gridRequest.SortBy)
                    ? null
                    : gridRequest.SortDescending,
                StockKeepingUnitId = _selectedStockKeepingUnit?.Id,
                WarehouseId = _selectedWarehouseId,
                StorageLocationId = _selectedStorageLocation?.Id,
                TransactionType = string.IsNullOrWhiteSpace(_selectedTransactionType)
                    ? null
                    : _selectedTransactionType,
                OccurredFromUtc = occurredFromUtc,
                OccurredToUtc = occurredToUtc
            };

            ListResult<InventoryLedgerEntryDetails> result =
                await WmsInventoryApiClient.ListInventoryLedgerEntriesAsync(request, cancellationToken);

            return new GridData<InventoryLedgerEntryDetails>
            {
                Items = result.Items,
                TotalItems = result.TotalCount
            };
        }
        catch (Exception exception)
            when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _errorMessage = exception.Message;
            return EmptyGridData();
        }
    }

    private async Task OpenTransactionDetailsAsync(InventoryLedgerEntryDetails entry)
    {
        _errorMessage = null;

        try
        {
            InventoryTransactionDetails transaction = await WmsInventoryApiClient
                .GetInventoryTransactionByIdAsync(entry.TransactionId);

            DialogParameters parameters = new()
            {
                [nameof(InventoryTransactionDetailsDialog.Transaction)] = transaction
            };

            DialogOptions options = new()
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Large,
                FullWidth = true
            };

            await DialogService.ShowAsync<InventoryTransactionDetailsDialog>(
                "Inventory transaction details",
                parameters,
                options);
        }
        catch (Exception exception)
        {
            _errorMessage = exception.Message;
        }
    }

    private Task ReloadInventoryLedgerAsync()
    {
        return _inventoryLedgerGrid?.ReloadServerDataAsync()
            ?? Task.CompletedTask;
    }

    private Task ResetAndReloadInventoryLedgerAsync()
    {
        return _inventoryLedgerGrid?.ResetAndReloadServerDataAsync()
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
                IncludeInactive: true);

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

    private async Task HydrateRoutedFiltersAsync()
    {
        _routeValidationMessage = null;
        _isRouteStateBlockingLedgerRequests = false;

        try
        {
            StorageLocationDetails? routedStorageLocation = null;

            if (RoutedStorageLocationId is Guid storageLocationId)
            {
                routedStorageLocation = await WmsTopologyApiClient.GetStorageLocationByIdAsync(storageLocationId);
                _selectedStorageLocation = new StorageLocationLookupItem(
                    routedStorageLocation.Id,
                    routedStorageLocation.WarehouseId,
                    routedStorageLocation.Code,
                    routedStorageLocation.Name,
                    routedStorageLocation.IsActive);
            }

            Guid? resolvedWarehouseId = RoutedWarehouseId ?? routedStorageLocation?.WarehouseId;

            if (resolvedWarehouseId is Guid warehouseId)
            {
                WarehouseDetails warehouse = await ResolveWarehouseAsync(warehouseId);
                _selectedWarehouseId = warehouse.Id;
            }

            if (RoutedWarehouseId is Guid routeWarehouseId &&
                routedStorageLocation is not null &&
                routedStorageLocation.WarehouseId != routeWarehouseId)
            {
                _routeValidationMessage =
                    "The routed warehouse and storage location do not match. Correct or clear the filters before loading ledger history.";
                _isRouteStateBlockingLedgerRequests = true;
                return;
            }

            if (RoutedStockKeepingUnitId is Guid stockKeepingUnitId)
            {
                _selectedStockKeepingUnit = await ResolveStockKeepingUnitLookupItemAsync(stockKeepingUnitId);
            }
        }
        catch (OperationCanceledException)
        {
            _isRouteStateBlockingLedgerRequests = true;
        }
        catch (Exception exception)
        {
            _routeValidationMessage =
                $"The routed ledger filters could not be restored: {exception.Message}";
            _isRouteStateBlockingLedgerRequests = true;
        }
    }

    private async Task<WarehouseDetails> ResolveWarehouseAsync(Guid warehouseId)
    {
        WarehouseDetails? warehouse = _warehouses.SingleOrDefault(x => x.Id == warehouseId);

        if (warehouse is not null)
        {
            return warehouse;
        }

        warehouse = await WmsTopologyApiClient.GetWarehouseByIdAsync(warehouseId);
        _warehouses.Add(warehouse);
        _warehouses = _warehouses
            .OrderBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return warehouse;
    }

    private async Task<StockKeepingUnitLookupItem> ResolveStockKeepingUnitLookupItemAsync(Guid stockKeepingUnitId)
    {
        StockKeepingUnitDetails sku = await WmsCatalogApiClient.GetStockKeepingUnitByIdAsync(stockKeepingUnitId);
        UnitOfMeasureDetails baseUom = await WmsCatalogApiClient.GetUnitOfMeasureByIdAsync(sku.BaseUnitOfMeasureId);

        return new StockKeepingUnitLookupItem(
            sku.Id,
            sku.Code,
            sku.Name,
            baseUom.Id,
            baseUom.Code,
            baseUom.Symbol,
            sku.IsActive,
            baseUom.IsActive);
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

    private bool TryCreateOccurrenceRange(
        out DateTimeOffset? occurredFromUtc,
        out DateTimeOffset? occurredToUtc,
        out string? validationMessage)
    {
        occurredFromUtc = null;
        occurredToUtc = null;
        validationMessage = null;

        if (!TryParseUtcBoundary(
                _occurredFromUtcText,
                "Occurred from UTC",
                out occurredFromUtc,
                out validationMessage))
        {
            return false;
        }

        if (!TryParseUtcBoundary(
                _occurredToUtcText,
                "Occurred to UTC",
                out occurredToUtc,
                out validationMessage))
        {
            return false;
        }

        if (occurredFromUtc.HasValue &&
            occurredToUtc.HasValue &&
            occurredFromUtc.Value > occurredToUtc.Value)
        {
            validationMessage = "Occurred from UTC must be earlier than or equal to occurred to UTC.";
            return false;
        }

        validationMessage = null;
        return true;
    }

    private static bool TryParseUtcBoundary(
        string? text,
        string fieldName,
        out DateTimeOffset? value,
        out string? validationMessage)
    {
        value = null;
        validationMessage = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        if (DateTimeOffset.TryParse(
                text,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out DateTimeOffset parsed))
        {
            value = parsed.ToUniversalTime();
            return true;
        }

        validationMessage = $"{fieldName} must be an exact UTC date/time, for example 2026-06-18T09:00:00Z.";
        return false;
    }

    private void ClearRouteBlockingState()
    {
        _routeValidationMessage = null;
        _isRouteStateBlockingLedgerRequests = false;
    }

    private static GridData<InventoryLedgerEntryDetails> EmptyGridData()
    {
        return new GridData<InventoryLedgerEntryDetails>
        {
            Items = [],
            TotalItems = 0
        };
    }
}
