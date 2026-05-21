using Myrmex.Core.Results;

namespace Myrmex.Core.Application;

public interface ICommandHandler<TCommand, TResult> : IHandler
    where TCommand : ICommand<TResult>
    where TResult : IServiceResult
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
