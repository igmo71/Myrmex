using Microsoft.AspNetCore.Components;
using MudBlazor;
using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Catalog;
using Myrmex.WebApp.Wms.Api;
using Myrmex.WebApp.Wms.Catalog;

namespace Myrmex.WebApp.Components.Pages.Wms.Catalog.SkuPages;

public partial class Index
{
    [Inject]
    private WmsCatalogApiClient WmsCatalogApiClient { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    private SkuGrid? _skuGrid;
    private IReadOnlyDictionary<Guid, UnitOfMeasureDetails> _unitOfMeasureLookup =
        new Dictionary<Guid, UnitOfMeasureDetails>();

    private string? _errorMessage;
    private string? _searchText;
    private bool _includeInactive;

    protected override async Task OnInitializedAsync()
    {
        await LoadUnitOfMeasureLookupAsync();
    }

    private Task ReloadAsync()
    {
        return _skuGrid?.ReloadServerDataAsync() ?? Task.CompletedTask;
    }

    private async Task OnSearchTextChanged(string? value)
    {
        _searchText = value;
        await ResetAndReloadSkusAsync();
    }

    private async Task OnIncludeInactiveChanged(bool value)
    {
        _includeInactive = value;
        await ResetAndReloadSkusAsync();
    }

    private async Task<GridData<StockKeepingUnitDetails>> LoadSkusAsync(
        SkuGridRequest gridRequest,
        CancellationToken cancellationToken)
    {
        _errorMessage = null;

        try
        {
            ListStockKeepingUnitsRequest request = new()
            {
                Skip = gridRequest.Skip,
                Take = gridRequest.Take,
                SearchText = _searchText,
                SortBy = gridRequest.SortBy,
                SortDescending = gridRequest.SortDescending,
                IncludeInactive = _includeInactive
            };

            ListResult<StockKeepingUnitDetails> result = await WmsCatalogApiClient
                .ListStockKeepingUnitsAsync(request, cancellationToken);

            return new GridData<StockKeepingUnitDetails>
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

    private async Task LoadUnitOfMeasureLookupAsync()
    {
        try
        {
            ListResult<UnitOfMeasureDetails> result = await WmsCatalogApiClient
                .ListUnitsOfMeasureAsync(new ListUnitsOfMeasureRequest
                {
                    Skip = 0,
                    Take = 100,
                    SortBy = UnitOfMeasureSortBy.Code,
                    SortDescending = false,
                    IncludeInactive = false
                });

            _unitOfMeasureLookup = result.Items.ToDictionary(unit => unit.Id);
        }
        catch (Exception exception)
        {
            _errorMessage = exception.Message;
            _unitOfMeasureLookup = new Dictionary<Guid, UnitOfMeasureDetails>();
        }
    }

    private Task ResetAndReloadSkusAsync()
    {
        return _skuGrid?.ResetAndReloadServerDataAsync() ?? Task.CompletedTask;
    }

    private async Task CreateSkuAsync()
    {
        DialogOptions options = new()
        {
            CloseButton = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        IDialogReference dialog = await DialogService
            .ShowAsync<SkuEditDialog>(Localizer["Sku.CreateTitle"], options);

        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        Snackbar.Add(Localizer["Sku.Created"], Severity.Success);

        await ReloadAsync();
    }

    private async Task EditSkuAsync(StockKeepingUnitDetails sku)
    {
        DialogParameters parameters = new()
        {
            [nameof(SkuEditDialog.Sku)] = sku
        };

        DialogOptions options = new()
        {
            CloseButton = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        IDialogReference dialog = await DialogService
            .ShowAsync<SkuEditDialog>(Localizer["Sku.EditTitle"], parameters, options);

        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        Snackbar.Add(Localizer["Sku.Updated"], Severity.Success);

        await ReloadAsync();
    }

    private async Task DeactivateSkuAsync(StockKeepingUnitDetails sku)
    {
        try
        {
            ApiResult<StockKeepingUnitDetails> result = await WmsCatalogApiClient
                .TryDeactivateStockKeepingUnitAsync(sku.Id);

            if (result.IsFailure)
            {
                Snackbar.Add(result.Error?.Message ?? Localizer["Sku.DeactivateError"], Severity.Error);

                return;
            }

            Snackbar.Add(Localizer["Sku.Deactivated"], Severity.Success);

            await ReloadAsync();
        }
        catch (Exception exception)
        {
            Snackbar.Add(exception.Message, Severity.Error);
        }
    }

    private async Task ReactivateSkuAsync(StockKeepingUnitDetails sku)
    {
        try
        {
            ApiResult<StockKeepingUnitDetails> result = await WmsCatalogApiClient
                .TryReactivateStockKeepingUnitAsync(sku.Id);

            if (result.IsFailure)
            {
                Snackbar.Add(result.Error?.Message ?? Localizer["Sku.ReactivateError"], Severity.Error);

                return;
            }

            Snackbar.Add(Localizer["Sku.Reactivated"], Severity.Success);

            await ReloadAsync();
        }
        catch (Exception exception)
        {
            Snackbar.Add(exception.Message, Severity.Error);
        }
    }

    private static GridData<StockKeepingUnitDetails> EmptyGridData()
    {
        return new GridData<StockKeepingUnitDetails>
        {
            Items = [],
            TotalItems = 0
        };
    }
}
