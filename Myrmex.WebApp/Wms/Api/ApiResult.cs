namespace Myrmex.WebApp.Wms.Api;

public sealed record ApiResult<T>
{
    private ApiResult(T? value, ApiError? error)
    {
        Value = value;
        Error = error;
    }

    public T? Value { get; }

    public ApiError? Error { get; }

    public bool IsSuccess => Error is null;

    public bool IsFailure => Error is not null;

    public static ApiResult<T> Success(T value)
    {
        return new ApiResult<T>(value, error: null);
    }

    public static ApiResult<T> Failure(ApiError error)
    {
        return new ApiResult<T>(value: default, error);
    }
}

public sealed record ApiError(
    int? Status,
    string Message,
    IReadOnlyDictionary<string, string> Extensions)
{
    public string? Code => Extensions.TryGetValue("code", out string? code)
        ? code
        : null;

    public static ApiError Create(
        int? status,
        string message,
        IReadOnlyDictionary<string, string>? extensions = null)
    {
        return new ApiError(
            status,
            message,
            extensions ?? new Dictionary<string, string>());
    }
}
