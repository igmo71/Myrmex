namespace Myrmex.Core.Common;

public sealed record DomainValidationFailure(string Code, string Message, string? Field = null);