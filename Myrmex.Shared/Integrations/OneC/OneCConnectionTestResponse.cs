namespace Myrmex.Shared.Integrations.OneC;

public sealed record OneCConnectionTestResponse(
    DateTimeOffset CheckedAtUtc,
    bool IsReady,
    IReadOnlyList<string> CheckedReferenceTypes);
