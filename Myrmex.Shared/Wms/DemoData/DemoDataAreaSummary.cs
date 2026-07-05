namespace Myrmex.Shared.Wms.DemoData;

public sealed record DemoDataAreaSummary(
    string Area,
    int Created,
    int Reused,
    int Skipped,
    int Deleted);
