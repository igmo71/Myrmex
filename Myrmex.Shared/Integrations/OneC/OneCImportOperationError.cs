namespace Myrmex.Shared.Integrations.OneC;

public sealed record OneCImportOperationError(
    string Reason,
    string Message);
