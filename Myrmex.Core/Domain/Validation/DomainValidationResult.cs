namespace Myrmex.Core.Domain.Validation;

public sealed record DomainValidationResult(IReadOnlyList<DomainValidationFailure> Errors)
{
    public bool IsValid => Errors.Count == 0;

    public static DomainValidationResult Valid { get; } = new([]);

    public static DomainValidationResult Invalid(params DomainValidationFailure[] errors)
        => errors.Length == 0 ? Valid : new(errors);

    public static DomainValidationResult Invalid(IEnumerable<DomainValidationFailure> errors)
    {
        var materializedErrors = errors.ToArray();

        return materializedErrors.Length == 0 ? Valid : new(materializedErrors);
    }
}
