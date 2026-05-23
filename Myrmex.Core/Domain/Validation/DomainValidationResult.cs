namespace Myrmex.Core.Domain.Validation;

public sealed record DomainValidationResult(IReadOnlyList<DomainValidationFailure> Errors)
{
    public bool IsValid => Errors.Count == 0;

    public static DomainValidationResult Valid { get; } = new([]);

    public static DomainValidationResult From(IEnumerable<DomainValidationFailure> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        DomainValidationFailure[] materializedErrors = [.. errors];

        return materializedErrors.Length == 0 ? Valid : new DomainValidationResult(materializedErrors);
    }

    public static DomainValidationResult Invalid(params DomainValidationFailure[] errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        if (errors.Length == 0)
        {
            throw new ArgumentException("Invalid validation result must contain at least one error.", nameof(errors));
        }

        return new DomainValidationResult(errors);
    }

    public static DomainValidationResult Invalid(IEnumerable<DomainValidationFailure> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        DomainValidationFailure[] materializedErrors = [.. errors];

        if (materializedErrors.Length == 0)
        {
            throw new ArgumentException("Invalid validation result must contain at least one error.", nameof(errors));
        }

        return new DomainValidationResult(materializedErrors);
    }
}