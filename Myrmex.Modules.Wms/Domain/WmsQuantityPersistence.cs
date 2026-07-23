using Myrmex.Core.Domain.Validation;

namespace Myrmex.Modules.Wms.Domain;

internal static class WmsQuantityPersistence
{
    public const int Precision = 18;
    public const int Scale = 4;
    public const decimal MaximumValue = 99_999_999_999_999.9999m;
    public const decimal MinimumValue = -MaximumValue;

    public static bool IsExactlyRepresentable(decimal value)
    {
        return value is >= MinimumValue and <= MaximumValue &&
            decimal.Round(value, Scale) == value;
    }

    public static DomainValidationFailure? Validate<TEntity>(
        decimal value,
        string property)
    {
        return IsExactlyRepresentable(value)
            ? null
            : new DomainValidationFailure(
                $"PersistenceRange-{typeof(TEntity).Name}-{property}",
                $"{property} must be exactly representable as decimal({Precision},{Scale}).",
                property);
    }
}
