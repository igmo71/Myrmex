using Myrmex.Core.Validation;
using System.Diagnostics.CodeAnalysis;

namespace Myrmex.Core.Results;

public class ServiceResult : IServiceResult
{
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess { get; }

    public ServiceError? Error { get; }

    protected ServiceResult()
    {
        IsSuccess = true;
        Error = null;
    }

    protected ServiceResult(ServiceError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        IsSuccess = false;
        Error = error;
    }

    public static ServiceResult Success() => new();

    public static ServiceResult Fail(ServiceError error) => new(error);

    public static ServiceResult Fail() => new(ServiceError.Unknown);

    public static ServiceResult Invalid(IEnumerable<DomainValidationFailure> failures)
    {
        ArgumentNullException.ThrowIfNull(failures);

        ServiceError[] details = failures
            .Select(f => new ServiceError(ServiceErrorType.Invalid, f.Code, f.Message, f.Field))
            .ToArray();

        if (details.Length == 0)
            return Fail();

        return Fail(new ServiceError(
            ServiceErrorType.Invalid,
            "Validation.Invalid",
            "One or more validation errors occurred.",
            Details: details));
    }

    public static implicit operator ServiceResult(ServiceError error) => Fail(error);
}


public sealed class ServiceResult<TValue> : ServiceResult, IServiceResult<TValue>
{
    private readonly TValue? _value;

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value for failed result.");

    private ServiceResult(TValue value) : base()
    {
        ArgumentNullException.ThrowIfNull(value);

        _value = value;
    }

    private ServiceResult(ServiceError error) : base(error)
    {
        _value = default;
    }

    public static ServiceResult<TValue> Success(TValue value) => new(value);

    public static new ServiceResult<TValue> Fail(ServiceError error) => new(error);

    public static new ServiceResult<TValue> Fail() => new(ServiceError.Unknown);

    public static new ServiceResult<TValue> Invalid(IEnumerable<DomainValidationFailure> failures)
    {
        ArgumentNullException.ThrowIfNull(failures);

        ServiceError[] details = failures
            .Select(f => new ServiceError(ServiceErrorType.Invalid, f.Code, f.Message, f.Field))
            .ToArray();

        if (details.Length == 0)
            return Fail();

        return Fail(new ServiceError(
            ServiceErrorType.Invalid,
            "Validation.Invalid",
            "One or more validation errors occurred.",
            Details: details));
    }

    public static implicit operator ServiceResult<TValue>(TValue value) => Success(value);

    public static implicit operator ServiceResult<TValue>(ServiceError error) => Fail(error);
}
