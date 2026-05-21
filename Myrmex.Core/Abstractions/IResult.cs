namespace Myrmex.Core.Abstractions;

public interface IResult
{
}

public interface IResult<TValue> : IResult
{
    TValue Value { get; }
}