namespace Myrmex.Shared.Integrations.OneC;

public sealed record ReceivingOrderImportDocumentResult(
    Guid? ExternalRefKey,
    string? Number,
    DateTime? Date,
    string Outcome,
    string? Reason,
    string? Message);
