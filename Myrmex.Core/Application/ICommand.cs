using Myrmex.Core.Results;

namespace Myrmex.Core.Application;

public interface ICommand : IRequest
{
}

public interface ICommand<TResult> : ICommand
    where TResult : IServiceResult
{
}
