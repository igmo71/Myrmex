using Microsoft.Extensions.DependencyInjection;
using Myrmex.Core.Application;
using Myrmex.Core.Results;

namespace Myrmex.AppDispatching.QueryDispatching;

internal sealed class QueryDispatcher(IServiceProvider serviceProvider) : IQueryDispatcher
{
    public Task<TResult> DispatchAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResult>
        where TResult : IServiceResult
    {
        ArgumentNullException.ThrowIfNull(query);

        IQueryHandler<TQuery, TResult> handler = serviceProvider.GetRequiredService<IQueryHandler<TQuery, TResult>>();

        return handler.HandleAsync(query, cancellationToken);
    }
}