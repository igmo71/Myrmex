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
    public static ServiceError NotFound<TEntity>(string? field = null)
        => new(ServiceErrorType.NotFound, $"{typeof(TEntity).Name}-NotFound", $"{typeof(TEntity).Name} not found.", field);

    public static ServiceError Conflict<TEntity>(string? field = null, string? message = null)
        => new(ServiceErrorType.Conflict, $"{typeof(TEntity).Name}-Conflict", message ?? $"{typeof(TEntity).Name} is in conflict.", field);

    public static ServiceError Validation<TEntity>(string? field = null)
        => new(ServiceErrorType.Invalid, $"{typeof(TEntity).Name}-Validation", $"{typeof(TEntity).Name} has validation errors.", field);

    public static ServiceError Unauthorized()
        => new(ServiceErrorType.Unauthorized, "Unauthorized", "Authentication is required.");

    public static ServiceError Forbidden()
        => new(ServiceErrorType.Forbidden, "Forbidden", "Access is forbidden.");

    public static ServiceError Failure<TEntity>(string message, string? field = null)
        => new(ServiceErrorType.Failure, $"{typeof(TEntity).Name}-Failure", message, field);
}