using Myrmex.Core.Results;

namespace Myrmex.Core.Application;

public interface IQueryHandler<TQuery, TResult> : IHandler
    where TQuery : IQuery<TResult>
    where TResult : IServiceResult
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
