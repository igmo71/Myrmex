namespace Myrmex.Shared.Integrations.OneC;

public sealed record OneCImportRecordError(
    Guid? ExternalRefKey,
    string? Code,
    string Reason,
    string Message);
