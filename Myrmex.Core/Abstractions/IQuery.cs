namespace Myrmex.Core.Abstractions;

public interface IQuery : IRequest
{
}

public interface IQuery<TResult> : IQuery
    where TResult : IResult
{
}