namespace Myrmex.Core.Results;

public sealed record ServiceError(
    ServiceErrorType Type,
    string Code,
    string Message,
    string? Field = null,
    IReadOnlyList<ServiceError>? Details = null)
{
    public IReadOnlyList<ServiceError> DetailList => Details ?? [];

    public static ServiceError Unknown { get; } = new(ServiceErrorType.Unknown, "Error.Unknown", "An unknown error occurred.");
}