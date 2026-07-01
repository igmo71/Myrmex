using Microsoft.Extensions.Localization;
using Myrmex.WebApp.Localization;

namespace Myrmex.WebApp.Wms.Topology;

internal static class WarehouseDisplayFormatter
{
    public static string GetName(
        string? name,
        IStringLocalizer<SharedResource> localizer)
    {
        return string.IsNullOrWhiteSpace(name)
            ? localizer["Common.NotAvailable"]
            : name;
    }
}
