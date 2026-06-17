using Microsoft.AspNetCore.Components;
using MudBlazor;
using Myrmex.Shared.Common;
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

    private List<StockKeepingUnitDetails> _skus = [];
    private IReadOnlyDictionary<Guid, UnitOfMeasureDetails> _unitOfMeasureLookup =
        new Dictionary<Guid, UnitOfMeasureDetails>();

    private bool _isLoading;
    private string? _errorMessage;
    private string? _searchText;
    private bool _includeInactive;

    protected override async Task OnInitializedAsync()
    {
        await LoadSkusAsync();
    }

    private async Task ReloadAsync()
    {
        await LoadSkusAsync();
    }

    private async Task OnSearchTextChanged(string? value)
    {
        _searchText = value;
        await LoadSkusAsync();
    }

    private async Task OnIncludeInactiveChanged(bool value)
    {
        _includeInactive = value;
        await LoadSkusAsync();
    }

    private async Task LoadSkusAsync()
    {
        _isLoading = true;
        _errorMessage = null;

        try
        {
            ListRequest skuRequest = new(
                Skip: 0,
                Take: 100,
                SearchText: _searchText,
                SortBy: "code",
                SortDescending: false,
                IncludeInactive: _includeInactive);

            ListRequest unitOfMeasureRequest = new(
                Skip: 0,
                Take: 100,
                SortBy: "code",
                SortDescending: false,
                IncludeInactive: false);

            ListResult<StockKeepingUnitDetails> skuResult = await WmsCatalogApiClient
                .ListStockKeepingUnitsAsync(skuRequest);

            ListResult<UnitOfMeasureDetails> unitOfMeasureResult = await WmsCatalogApiClient
                .ListUnitsOfMeasureAsync(unitOfMeasureRequest);

            _skus = skuResult.Items.ToList();
            _unitOfMeasureLookup = unitOfMeasureResult.Items.ToDictionary(unitOfMeasure => unitOfMeasure.Id);
        }
        catch (Exception exception)
        {
            _errorMessage = exception.Message;
            _skus = [];
            _unitOfMeasureLookup = new Dictionary<Guid, UnitOfMeasureDetails>();
        }
        finally
        {
            _isLoading = false;
        }
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
            .ShowAsync<SkuEditDialog>("Create SKU", options);

        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        Snackbar.Add("SKU created.", Severity.Success);

        await LoadSkusAsync();
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
            .ShowAsync<SkuEditDialog>("Edit SKU", parameters, options);

        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        Snackbar.Add("SKU updated.", Severity.Success);

        await LoadSkusAsync();
    }

    private async Task DeactivateSkuAsync(StockKeepingUnitDetails sku)
    {
        try
        {
            ApiResult<StockKeepingUnitDetails> result = await WmsCatalogApiClient
                .TryDeactivateStockKeepingUnitAsync(sku.Id);

            if (result.IsFailure)
            {
                Snackbar.Add(result.Error?.Message ?? "SKU deactivation failed.", Severity.Error);

                return;
            }

            Snackbar.Add("SKU deactivated.", Severity.Success);

            await LoadSkusAsync();
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
                Snackbar.Add(result.Error?.Message ?? "SKU reactivation failed.", Severity.Error);

                return;
            }

            Snackbar.Add("SKU reactivated.", Severity.Success);

            await LoadSkusAsync();
        }
        catch (Exception exception)
        {
            Snackbar.Add(exception.Message, Severity.Error);
        }
    }
}
