namespace Myrmex.WebApp.Wms.Api;

public sealed record ListRequest(
    int Skip = 0,
    int Take = 20,
    string? SearchText = null,
    string? SortBy = null,
    bool SortDescending = false,
    bool IncludeInactive = false);

public sealed record ListResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Skip,
    int Take);
