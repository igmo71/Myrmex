using Microsoft.AspNetCore.Components;
using MudBlazor;
using Myrmex.Shared.Wms.Inventory;

namespace Myrmex.WebApp.Components.Pages.Wms.Inventory.InventoryTransferPages;

public partial class Index
{
    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    private readonly List<InventoryTransferDetails> _createdTransfers = [];
    private string? _errorMessage;

    private async Task OpenCreateTransferAsync()
    {
        DialogOptions options = new()
        {
            CloseButton = false,
            MaxWidth = MaxWidth.Large,
            FullWidth = true
        };

        IDialogReference dialog = await DialogService
            .ShowAsync<CreateInventoryTransferDialog>("Create transfer", options);

        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        if (result.Data is InventoryTransferDetails transfer)
        {
            _createdTransfers.Insert(0, transfer);
            Snackbar.Add("Inventory transfer created.", Severity.Success);
        }
    }

    private async Task OpenDetailsAsync(InventoryTransferDetails transfer)
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

    private void ReplaceTransfer(InventoryTransferDetails transfer)
    {
        int index = _createdTransfers.FindIndex(x => x.Id == transfer.Id);

        if (index >= 0)
        {
            _createdTransfers[index] = transfer;
        }
    }
}
