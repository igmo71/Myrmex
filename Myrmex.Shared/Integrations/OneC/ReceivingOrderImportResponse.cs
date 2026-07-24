namespace Myrmex.Shared.Integrations.OneC;

public sealed record ReceivingOrderImportResponse(
    int Processed,
    int Created,
    int Updated,
    int Skipped,
    int Failed,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    OneCImportOperationError? OperationError,
    IReadOnlyList<ReceivingOrderImportDocumentResult> Results);
