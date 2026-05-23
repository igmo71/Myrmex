namespace Myrmex.Core.Application.Queries;

public sealed record ListResult<TItem>(
    IReadOnlyList<TItem> Items,
    int TotalCount,
    int Skip,
    int Take);
