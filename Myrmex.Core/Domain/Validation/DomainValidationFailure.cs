namespace Myrmex.Core.Domain.Validation;

public sealed record DomainValidationFailure(string Code, string Message, string? Field = null);