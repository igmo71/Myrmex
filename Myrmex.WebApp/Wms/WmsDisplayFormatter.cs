using System.Globalization;
using Microsoft.Extensions.Localization;
using Myrmex.WebApp.Localization;

namespace Myrmex.WebApp.Wms;

internal static class WmsDisplayFormatter
{
    public static string FormatLocalDateTime(DateTimeOffset value) =>
        value.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.CurrentCulture);

    public static string FormatLocalDateTime(DateTimeOffset? value) =>
        value.HasValue ? FormatLocalDateTime(value.Value) : "—";

    public static string FormatQuantity(decimal value) =>
        value.ToString("0.##", CultureInfo.CurrentCulture);

    public static string FormatSignedQuantity(decimal value) =>
        value > 0 ? $"+{FormatQuantity(value)}" : FormatQuantity(value);

    public static string FormatUom(string? symbol, string? displayName, string? code, IStringLocalizer<SharedResource> localizer) =>
        !string.IsNullOrWhiteSpace(symbol) ? symbol : !string.IsNullOrWhiteSpace(displayName) ? displayName : !string.IsNullOrWhiteSpace(code) ? code : localizer["Common.NotAvailable"];

    public static string FormatTransactionType(string value, IStringLocalizer<SharedResource> localizer) => value switch
    {
        "Receiving" => localizer["InventoryTransaction.Type.Receiving"],
        "Transfer" => localizer["InventoryTransaction.Type.Transfer"],
        "Adjustment" => localizer["InventoryTransaction.Type.Adjustment"],
        _ => value
    };

    public static string FormatTransferStatus(string value, IStringLocalizer<SharedResource> localizer) => value switch
    {
        "Created" => localizer["InventoryTransfer.Status.Created"], "InProgress" => localizer["InventoryTransfer.Status.InProgress"], "Completed" => localizer["InventoryTransfer.Status.Completed"], _ => value
    };

    public static string FormatCountStatus(string value, IStringLocalizer<SharedResource> localizer) => value switch
    {
        "Draft" => localizer["InventoryCount.Status.Draft"], "InProgress" => localizer["InventoryCount.Status.InProgress"], "Completed" => localizer["InventoryCount.Status.Completed"], "Cancelled" => localizer["InventoryCount.Status.Cancelled"], _ => value
    };

    public static string FormatMovementMeaning(string value, IStringLocalizer<SharedResource> localizer) => value switch
    {
        "Direct" => localizer["InventoryTransfer.Movement.Direct"], "Pick" => localizer["InventoryTransfer.Movement.Pick"], "Place" => localizer["InventoryTransfer.Movement.Place"], "Movement" => localizer["InventoryTransfer.Movement.Generic"], _ => value
    };
}
