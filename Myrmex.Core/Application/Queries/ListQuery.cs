namespace Myrmex.Core.Application.Queries;

public abstract record ListQuery
{
    public int Skip { get; init; }

    public int Take { get; init; } = 20;

    public string? SearchText { get; init; }

    public string? SortBy { get; init; }

    public bool SortDescending { get; init; }

    public bool IncludeInactive { get; init; }


    //public const int DefaultTake = 20;
    //public const int MaxTake = 200;
}
