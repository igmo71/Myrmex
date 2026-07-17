namespace Myrmex.Integrations.OneC.References;

internal enum OneCReferenceType
{
    Warehouse = 0,
    UnitOfMeasure = 1,
    StockKeepingUnit = 2
}

internal enum ReferenceSynchronizationOutcome
{
    Applied = 0,
    Unchanged = 1,
    ControlledSkip = 2,
    NotFound = 3,
    Busy = 4,
    TransientFailure = 5,
    PermanentFailure = 6
}

internal sealed record ReferenceSynchronizationResult(
    OneCReferenceType ReferenceType,
    Guid ExternalRefKey,
    ReferenceSynchronizationOutcome Outcome,
    string? Reason,
    string? Message,
    bool RetrySuitable)
{
    public static ReferenceSynchronizationResult Success(
        OneCReferenceType referenceType,
        Guid externalRefKey,
        ReferenceSynchronizationOutcome outcome,
        string? reason = null) =>
        new(referenceType, externalRefKey, outcome, reason, Message: null, RetrySuitable: false);

    public static ReferenceSynchronizationResult Failure(
        OneCReferenceType referenceType,
        Guid externalRefKey,
        ReferenceSynchronizationOutcome outcome,
        string reason,
        string message,
        bool retrySuitable) =>
        new(referenceType, externalRefKey, outcome, reason, message, retrySuitable);

    public string Diagnostic =>
        $"{ReferenceType} synchronization for {ExternalRefKey:D} failed with {Reason ?? Outcome.ToString()}: {Message ?? "No diagnostic supplied."}";
}

internal static class ReferenceSynchronizationReasons
{
    public const string Applied = nameof(Applied);
    public const string Unchanged = nameof(Unchanged);
    public const string SourceFolder = nameof(SourceFolder);
    public const string SourceRecordDeletionMarked = nameof(SourceRecordDeletionMarked);
    public const string NotFound = nameof(NotFound);
    public const string Busy = nameof(Busy);
    public const string SourceUnavailable = nameof(SourceUnavailable);
    public const string Timeout = nameof(Timeout);
    public const string InvalidConfiguration = nameof(InvalidConfiguration);
    public const string AuthenticationFailed = nameof(AuthenticationFailed);
    public const string EntitySetUnavailable = nameof(EntitySetUnavailable);
    public const string MalformedSourceData = nameof(MalformedSourceData);
    public const string ValidationFailed = nameof(ValidationFailed);
    public const string BusinessConflict = nameof(BusinessConflict);
    public const string ApplicationFailure = nameof(ApplicationFailure);
    public const string InvalidRequest = nameof(InvalidRequest);
    public const string BaseUnitOfMeasureRepairUnavailable = nameof(BaseUnitOfMeasureRepairUnavailable);
    public const string BaseUnitOfMeasureRepairFailed = nameof(BaseUnitOfMeasureRepairFailed);
}
