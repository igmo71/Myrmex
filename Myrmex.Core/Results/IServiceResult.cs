using System.Diagnostics.CodeAnalysis;

namespace Myrmex.Core.Results;

public interface IServiceResult
{
    [MemberNotNullWhen(false, nameof(Error))]
    bool IsSuccess { get; }

    ServiceError? Error { get; }
}

public interface IServiceResult<TValue> : IServiceResult
{
    TValue Value { get; }
}