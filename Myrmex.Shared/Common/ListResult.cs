namespace Myrmex.Shared.Common;

public sealed record ListResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Skip,
    int Take);
