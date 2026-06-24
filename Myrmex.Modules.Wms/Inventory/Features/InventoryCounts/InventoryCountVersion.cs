using Myrmex.Core.Domain.Validation;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryCounts;

namespace Myrmex.Modules.Wms.Inventory.Features.InventoryCounts;

internal static class InventoryCountVersion
{
    private const int SqlServerRowVersionLength = 8;

    public static DomainValidationFailure? Parse(
        string? value,
        string property,
        out byte[]? version)
    {
        version = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return DomainValidationFailure.Required<InventoryCount>(property);
        }

        try
        {
            byte[] parsed = Convert.FromBase64String(value);

            if (parsed.Length != SqlServerRowVersionLength)
            {
                return DomainValidationFailure.IncorrectState<InventoryCount>(property);
            }

            version = parsed;
            return null;
        }
        catch (FormatException)
        {
            return DomainValidationFailure.IncorrectState<InventoryCount>(property);
        }
    }
}
