namespace Myrmex.Core.Results;

public sealed record ServiceError(
    ServiceErrorType Type,
    string Code,
    string Message,
    string? Property = null,
    IReadOnlyList<ServiceError>? Details = null)
{
    public IReadOnlyList<ServiceError> DetailList => Details ?? [];

    public static ServiceError Unknown { get; } = new(ServiceErrorType.Unknown, "Error.Unknown", "An unknown error occurred.");
    public static ServiceError NotFound<TEntity>(string? message = null, string? property = null)
        => new(ServiceErrorType.NotFound, $"NotFound-{typeof(TEntity).Name}-{property}", message ?? $"{typeof(TEntity).Name} not found.", property);

    public static ServiceError Conflict<TEntity>(string? message = null, string? property = null)
        => new(ServiceErrorType.Conflict, $"Conflict-{typeof(TEntity).Name}-{property}", message ?? $"{typeof(TEntity).Name} is in conflict.", property);

    public static ServiceError Validation<TEntity>(string? message = null, string? property = null)
        => new(ServiceErrorType.Invalid, $"Validation-{typeof(TEntity).Name}-{property}", message ?? $"{typeof(TEntity).Name} has validation errors.", property);

    public static ServiceError Unauthorized(string? message = null)
        => new(ServiceErrorType.Unauthorized, "Unauthorized", message ?? "Authentication is required.");

    public static ServiceError Forbidden(string? message = null)
        => new(ServiceErrorType.Forbidden, "Forbidden", message ?? "Access is forbidden.");

    public static ServiceError Failure<TEntity>(string message, string? property = null)
        => new(ServiceErrorType.Failure, $"Failure-{typeof(TEntity).Name}-{property}", message, property);
}