namespace Myrmex.Core.Abstractions;

public interface IQueryHandler<TQuery, TResult> : IHandler
    where TQuery : IQuery
    where TResult : IResult
{
    Task<TResult> HandleAsync(TQuery command);
}
