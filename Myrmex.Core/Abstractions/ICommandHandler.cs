namespace Myrmex.Core.Abstractions;

public interface ICommandHandler<TCommand, TResult> : IHandler
    where TCommand : ICommand
    where TResult : IResult
{
    Task<TResult> HandleAsync(TCommand command);
}
