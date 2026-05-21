namespace Myrmex.Core.Common;

public sealed record ServiceError(string Code, string Message, string? Field = null);
