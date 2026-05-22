using Myrmex.Core.Application;
using Myrmex.Core.Results;

namespace Myrmex.ApplicationDispatching.QueryDispatching;

public interface IQueryDispatcher
{
    Task<TResult> DispatchAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResult>
        where TResult : IServiceResult;
}
