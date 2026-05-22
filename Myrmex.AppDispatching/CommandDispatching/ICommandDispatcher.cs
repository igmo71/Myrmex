using Myrmex.Core.Application;
using Myrmex.Core.Results;

namespace Myrmex.AppDispatching.CommandDispatching;

public interface ICommandDispatcher
{
    Task<TResult> DispatchAsync<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResult>
        where TResult : IServiceResult;
}
