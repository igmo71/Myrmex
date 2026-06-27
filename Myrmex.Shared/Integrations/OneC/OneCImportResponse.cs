namespace Myrmex.Shared.Integrations.OneC;

public sealed record OneCImportResponse(
    string ReferenceType,
    bool IsComplete,
    int Processed,
    int Created,
    int Updated,
    int Skipped,
    int Failed,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    OneCImportOperationError? OperationError,
    IReadOnlyList<OneCImportRecordError> Errors);
