using Microsoft.AspNetCore.Components;
using MudBlazor;
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

    private List<UnitOfMeasureDetails> _unitsOfMeasure = [];
    private bool _isLoading;
    private string? _errorMessage;
    private string? _searchText;
    private bool _includeInactive;

    protected override async Task OnInitializedAsync()
    {
        await LoadUnitsOfMeasureAsync();
    }

    private async Task ReloadAsync()
    {
        await LoadUnitsOfMeasureAsync();
    }

    private async Task OnSearchTextChanged(string? value)
    {
        _searchText = value;
        await LoadUnitsOfMeasureAsync();
    }

    private async Task OnIncludeInactiveChanged(bool value)
    {
        _includeInactive = value;
        await LoadUnitsOfMeasureAsync();
    }

    private async Task LoadUnitsOfMeasureAsync()
    {
        _isLoading = true;
        _errorMessage = null;

        try
        {
            ListRequest request = new(
                Skip: 0,
                Take: 100,
                SearchText: _searchText,
                SortBy: "code",
                SortDescending: false,
                IncludeInactive: _includeInactive);

            ListResult<UnitOfMeasureDetails> result = await WmsCatalogApiClient
                .ListUnitsOfMeasureAsync(request);

            _unitsOfMeasure = result.Items.ToList();
        }
        catch (Exception exception)
        {
            _errorMessage = exception.Message;
            _unitsOfMeasure = [];
        }
        finally
        {
            _isLoading = false;
        }
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
            .ShowAsync<UomEditDialog>("Create UoM", options);

        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        Snackbar.Add("UoM created.", Severity.Success);

        await LoadUnitsOfMeasureAsync();
    }
}
