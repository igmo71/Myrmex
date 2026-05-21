using Myrmex.Core.Abstractions;
using System.Diagnostics.CodeAnalysis;

namespace Myrmex.Core.Common;

public record ServiceResult(ServiceResultStatus Status, IReadOnlyList<ServiceError>? Errors = null) : IResult
{
    [MemberNotNullWhen(false, nameof(Errors))]
    public bool IsSuccess => Status == ServiceResultStatus.Success;

    public IReadOnlyList<ServiceError> ErrorList => Errors ?? [];

    public ServiceError? FirstError => ErrorList.FirstOrDefault();

    public static ServiceResult Success()
        => new(ServiceResultStatus.Success);

    public static ServiceResult ValidationFailed(IEnumerable<DomainValidationFailure> errors)
        => new(ServiceResultStatus.ValidationFailed, Errors: [.. errors.Select(error => new ServiceError(error.Code, error.Message, error.Field))]);

    public static ServiceResult Conflict(string code, string message, string? field = null)
        => new(ServiceResultStatus.Conflict, Errors: [new ServiceError(code, message, field)]);

    public static ServiceResult NotFound(string code, string message, string? field = null)
        => new(ServiceResultStatus.NotFound, Errors: [new ServiceError(code, message, field)]);
}


public sealed record ServiceResult<TValue> : ServiceResult, IResult<TValue>
{
    private readonly TValue? _value;

    private ServiceResult(ServiceResultStatus status, TValue? value = default, IReadOnlyList<ServiceError>? errors = null)
        : base(status, errors)
    {
        _value = value;
    }

    [MemberNotNullWhen(true, nameof(_value))]
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Operation failed with error: {Status}. Use the IsSuccess check.");


    public static ServiceResult<TValue> Success(TValue value)
        => new(ServiceResultStatus.Success, value);

    public static new ServiceResult<TValue> ValidationFailed(IEnumerable<DomainValidationFailure> errors)
        => new(ServiceResultStatus.ValidationFailed, errors: [.. errors.Select(error => new ServiceError(error.Code, error.Message, error.Field))]);

    public static new ServiceResult<TValue> Conflict(string code, string message, string? field = null)
        => new(ServiceResultStatus.Conflict, errors: [new ServiceError(code, message, field)]);

    public static new ServiceResult<TValue> NotFound(string code, string message, string? field = null)
        => new(ServiceResultStatus.NotFound, errors: [new ServiceError(code, message, field)]);
}
