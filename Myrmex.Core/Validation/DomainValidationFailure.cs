namespace Myrmex.Core.Validation;

public sealed record DomainValidationFailure(
    string Code,
    string Message,
    string? Field = null);