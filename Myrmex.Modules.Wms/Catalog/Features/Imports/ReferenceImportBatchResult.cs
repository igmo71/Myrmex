namespace Myrmex.Modules.Wms.Catalog.Features.Imports;

public sealed record ReferenceImportBatchResult(
    int Processed,
    int Created,
    int Updated,
    int Unchanged,
    int Skipped,
    int Failed,
    IReadOnlyList<ReferenceImportRecordError> Errors)
{
    public ReferenceImportBatchResult(
        int Processed,
        int Created,
        int Updated,
        int Skipped,
        int Failed,
        IReadOnlyList<ReferenceImportRecordError> Errors)
        : this(Processed, Created, Updated, 0, Skipped, Failed, Errors)
    {
    }

    public bool HasConsistentCounts => Processed == Created + Updated + Unchanged + Skipped + Failed;
}

public sealed record ReferenceImportRecordError(
    Guid? ExternalRefKey,
    string? Code,
    string Reason,
    string Message);

public static class ReferenceImportRecordErrorReasons
{
    public const string InvalidSourceRecord = nameof(InvalidSourceRecord);
    public const string SourceFolder = nameof(SourceFolder);
    public const string SourceRecordDeletionMarked = nameof(SourceRecordDeletionMarked);
    public const string CodeAlreadyExistsWithoutExternalRefKey = nameof(CodeAlreadyExistsWithoutExternalRefKey);
    public const string CodeAlreadyUsedByAnotherRecord = nameof(CodeAlreadyUsedByAnotherRecord);
    public const string BaseUnitOfMeasureExternalRefKeyMissing = nameof(BaseUnitOfMeasureExternalRefKeyMissing);
    public const string BaseUnitOfMeasureNotImported = nameof(BaseUnitOfMeasureNotImported);
    public const string BaseUnitOfMeasureInactive = nameof(BaseUnitOfMeasureInactive);
    public const string DeletionNotSupported = nameof(DeletionNotSupported);
}
