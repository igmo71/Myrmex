namespace Myrmex.Core.Application.Queries;

public abstract record ActiveListQuery : ListQuery
{
    public bool IncludeInactive { get; init; }
}
