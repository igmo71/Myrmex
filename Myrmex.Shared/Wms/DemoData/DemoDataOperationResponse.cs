namespace Myrmex.Shared.Wms.DemoData;

public sealed record DemoDataOperationResponse(
    string Operation,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    IReadOnlyList<DemoDataAreaSummary> Areas);
