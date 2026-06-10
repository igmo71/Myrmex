namespace Myrmex.Core.Results;

public static class ServiceErrors
{
    public static ServiceError NotFound(string code, string message, string? field = null)
        => new(ServiceErrorType.NotFound, code, message, field);

    public static ServiceError Conflict(string code, string message, string? field = null)
        => new(ServiceErrorType.Conflict, code, message, field);

    public static ServiceError Validation(string code, string message, string? field = null)
        => new(ServiceErrorType.Invalid, code, message, field);

    public static ServiceError Unauthorized(string code = "Auth.Unauthorized", string message = "Authentication is required.")
        => new(ServiceErrorType.Unauthorized, code, message);

    public static ServiceError Forbidden(string code = "Auth.Forbidden", string message = "Access is forbidden.")
        => new(ServiceErrorType.Forbidden, code, message);

    public static ServiceError Failure(string code, string message)
        => new(ServiceErrorType.Failure, code, message);
}
