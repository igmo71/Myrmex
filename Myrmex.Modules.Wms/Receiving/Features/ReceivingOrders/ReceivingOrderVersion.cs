using Myrmex.Core.Domain.Validation;
using Myrmex.Modules.Wms.Receiving.Domain.ReceivingOrders;

namespace Myrmex.Modules.Wms.Receiving.Features.ReceivingOrders;

internal static class ReceivingOrderVersion
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
            return DomainValidationFailure.Required<ReceivingOrder>(property);
        }

        try
        {
            byte[] parsed = Convert.FromBase64String(value);

            if (parsed.Length != SqlServerRowVersionLength)
            {
                return DomainValidationFailure.IncorrectState<ReceivingOrder>(property);
            }

            version = parsed;
            return null;
        }
        catch (FormatException)
        {
            return DomainValidationFailure.IncorrectState<ReceivingOrder>(property);
        }
    }
}
