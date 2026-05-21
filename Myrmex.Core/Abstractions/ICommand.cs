namespace Myrmex.Core.Abstractions;

public interface ICommand : IRequest
{
}

public interface ICommand<Tresult> : ICommand
    where Tresult : IResult
{
}
