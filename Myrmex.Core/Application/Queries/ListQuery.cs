namespace Myrmex.Core.Application.Queries;

public abstract record ListQuery
{
    public const int DefaultTake = 20;
    public const int MaxTake = 200;

    public int Skip { get; init; }

    public int Take { get; init; } = DefaultTake;

    public string? SearchText { get; init; }

    public string? SortBy { get; init; }

    public bool SortDescending { get; init; }
}
