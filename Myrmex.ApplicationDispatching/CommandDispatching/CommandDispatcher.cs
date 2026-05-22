using Microsoft.Extensions.DependencyInjection;
using Myrmex.Core.Application;
using Myrmex.Core.Results;

namespace Myrmex.ApplicationDispatching.CommandDispatching;

internal sealed class CommandDispatcher(
    IServiceProvider serviceProvider) : ICommandDispatcher
{
    public Task<TResult> DispatchAsync<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResult>
        where TResult : IServiceResult
    {
        ArgumentNullException.ThrowIfNull(command);

        ICommandHandler<TCommand, TResult> handler = serviceProvider.GetRequiredService<ICommandHandler<TCommand, TResult>>();

        return handler.HandleAsync(command, cancellationToken);
    }
}
