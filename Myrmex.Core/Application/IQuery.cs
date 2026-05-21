using Myrmex.Core.Results;

namespace Myrmex.Core.Application;

public interface IQuery : IRequest
{
}

public interface IQuery<TResult> : IQuery
    where TResult : IServiceResult
{
}