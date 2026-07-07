using Microsoft.AspNetCore.Components;
using MudBlazor;
using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Catalog;
using Myrmex.WebApp.Wms.Api;
using Myrmex.WebApp.Wms.Catalog;

namespace Myrmex.WebApp.Components.Pages.Wms.Catalog.UomPages;

public partial class Index
{
    [Inject]
    private WmsCatalogApiClient WmsCatalogApiClient { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    private UomGrid? _uomGrid;
    private string? _errorMessage;
    private string? _searchText;
    private bool _includeInactive;

    private Task ReloadAsync()
    {
        return _uomGrid?.ReloadServerDataAsync() ?? Task.CompletedTask;
    }

    private async Task OnSearchTextChanged(string? value)
    {
        _searchText = value;
        await ResetAndReloadUnitsOfMeasureAsync();
    }

    private async Task OnIncludeInactiveChanged(bool value)
    {
        _includeInactive = value;
        await ResetAndReloadUnitsOfMeasureAsync();
    }

    private async Task<GridData<UnitOfMeasureDetails>> LoadUnitsOfMeasureAsync(
        UomGridRequest gridRequest,
        CancellationToken cancellationToken)
    {
        _errorMessage = null;

        try
        {
            ListUnitsOfMeasureRequest request = new()
            {
                Skip = gridRequest.Skip,
                Take = gridRequest.Take,
                SearchText = _searchText,
                SortBy = gridRequest.SortBy,
                SortDescending = gridRequest.SortDescending,
                IncludeInactive = _includeInactive
            };

            ListResult<UnitOfMeasureDetails> result = await WmsCatalogApiClient
                .ListUnitsOfMeasureAsync(request, cancellationToken);

            return new GridData<UnitOfMeasureDetails>
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

    private Task ResetAndReloadUnitsOfMeasureAsync()
    {
        return _uomGrid?.ResetAndReloadServerDataAsync() ?? Task.CompletedTask;
    }

    private async Task CreateUnitOfMeasureAsync()
    {
        DialogOptions options = new()
        {
            CloseButton = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        IDialogReference dialog = await DialogService
            .ShowAsync<UomEditDialog>(Localizer["UnitOfMeasure.CreateTitle"], options);

        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        Snackbar.Add(Localizer["UnitOfMeasure.Created"], Severity.Success);

        await ReloadAsync();
    }

    private async Task EditUnitOfMeasureAsync(UnitOfMeasureDetails unitOfMeasure)
    {
        DialogParameters parameters = new()
        {
            [nameof(UomEditDialog.UnitOfMeasure)] = unitOfMeasure
        };

        DialogOptions options = new()
        {
            CloseButton = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        IDialogReference dialog = await DialogService
            .ShowAsync<UomEditDialog>(Localizer["UnitOfMeasure.EditTitle"], parameters, options);

        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        Snackbar.Add(Localizer["UnitOfMeasure.Updated"], Severity.Success);

        await ReloadAsync();
    }

    private async Task DeactivateUnitOfMeasureAsync(UnitOfMeasureDetails unitOfMeasure)
    {
        try
        {
            ApiResult<UnitOfMeasureDetails> result = await WmsCatalogApiClient
                .TryDeactivateUnitOfMeasureAsync(unitOfMeasure.Id);

            if (result.IsFailure)
            {
                Snackbar.Add(result.Error?.Message ?? Localizer["UnitOfMeasure.DeactivateError"], Severity.Error);

                return;
            }

            Snackbar.Add(Localizer["UnitOfMeasure.Deactivated"], Severity.Success);

            await ReloadAsync();
        }
        catch (Exception exception)
        {
            Snackbar.Add(exception.Message, Severity.Error);
        }
    }

    private async Task ReactivateUnitOfMeasureAsync(UnitOfMeasureDetails unitOfMeasure)
    {
        try
        {
            ApiResult<UnitOfMeasureDetails> result = await WmsCatalogApiClient
                .TryReactivateUnitOfMeasureAsync(unitOfMeasure.Id);

            if (result.IsFailure)
            {
                Snackbar.Add(result.Error?.Message ?? Localizer["UnitOfMeasure.ReactivateError"], Severity.Error);

                return;
            }

            Snackbar.Add(Localizer["UnitOfMeasure.Reactivated"], Severity.Success);

            await ReloadAsync();
        }
        catch (Exception exception)
        {
            Snackbar.Add(exception.Message, Severity.Error);
        }
    }

    private static GridData<UnitOfMeasureDetails> EmptyGridData()
    {
        return new GridData<UnitOfMeasureDetails>
        {
            Items = [],
            TotalItems = 0
        };
    }
}
