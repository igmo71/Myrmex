using Myrmex.Integrations.OneC.Common.Transport;
using Myrmex.Modules.Wms.Catalog.Features.Imports;
using Myrmex.Shared.Integrations.OneC;

namespace Myrmex.Integrations.OneC.Common.Imports;

internal sealed class OneCImportResponseFactory(TimeProvider timeProvider)
{
    internal const int MaximumReturnedErrors = 50;

    public OneCImportResponse CompleteFromBatch(
        string referenceType,
        DateTimeOffset startedAtUtc,
        ReferenceImportBatchResult batch,
        IReadOnlyList<OneCImportRecordError> pendingErrors)
    {
        List<OneCImportRecordError> errors = pendingErrors
            .Concat(ConvertErrors(batch.Errors))
            .Take(MaximumReturnedErrors)
            .ToList();

        return Complete(
            referenceType,
            startedAtUtc,
            batch.Processed + pendingErrors.Count,
            batch.Created,
            batch.Updated,
            batch.Unchanged,
            batch.Skipped + pendingErrors.Count,
            batch.Failed,
            errors);
    }

    public OneCImportResponse Complete(
        string referenceType,
        DateTimeOffset startedAtUtc,
        int processed,
        int created,
        int updated,
        int unchanged,
        int skipped,
        int failed,
        IReadOnlyList<OneCImportRecordError> errors) =>
        new(
            ReferenceType: referenceType,
            IsComplete: true,
            Processed: processed,
            Created: created,
            Updated: updated,
            Unchanged: unchanged,
            Skipped: skipped,
            Failed: failed,
            StartedAtUtc: startedAtUtc,
            CompletedAtUtc: timeProvider.GetUtcNow(),
            OperationError: null,
            Errors: errors.Take(MaximumReturnedErrors).ToArray());

    public OneCImportResponse Incomplete(
        string referenceType,
        DateTimeOffset startedAtUtc,
        string reason,
        string message,
        int processed = 0,
        int created = 0,
        int updated = 0,
        int unchanged = 0,
        int skipped = 0,
        int failed = 0,
        IReadOnlyList<OneCImportRecordError>? errors = null) =>
        new(
            ReferenceType: referenceType,
            IsComplete: false,
            Processed: processed,
            Created: created,
            Updated: updated,
            Unchanged: unchanged,
            Skipped: skipped,
            Failed: failed,
            StartedAtUtc: startedAtUtc,
            CompletedAtUtc: timeProvider.GetUtcNow(),
            OperationError: new OneCImportOperationError(reason, message),
            Errors: (errors ?? []).Take(MaximumReturnedErrors).ToArray());

    public OneCImportResponse IncompleteFromTransport(
        string referenceType,
        DateTimeOffset startedAtUtc,
        OneCTransportFailureReason reason,
        int processed = 0,
        int created = 0,
        int updated = 0,
        int unchanged = 0,
        int skipped = 0,
        int failed = 0,
        IReadOnlyList<OneCImportRecordError>? errors = null) =>
        Incomplete(
            referenceType,
            startedAtUtc,
            OperationReason(reason),
            OperationMessage(reason),
            processed,
            created,
            updated,
            unchanged,
            skipped,
            failed,
            errors);

    public static IEnumerable<OneCImportRecordError> ConvertErrors(
        IEnumerable<ReferenceImportRecordError> errors) =>
        errors.Select(error => new OneCImportRecordError(
            error.ExternalRefKey,
            error.Code,
            error.Reason,
            error.Message));

    public static void AppendErrors(
        ICollection<OneCImportRecordError> target,
        IEnumerable<OneCImportRecordError> source)
    {
        foreach (OneCImportRecordError error in
            source.Take(MaximumReturnedErrors - target.Count))
        {
            target.Add(error);
        }
    }

    internal static string OperationReason(OneCTransportFailureReason reason) => reason switch
    {
        OneCTransportFailureReason.AuthenticationFailed => "AuthenticationFailed",
        OneCTransportFailureReason.EntitySetUnavailable => "EntitySetUnavailable",
        OneCTransportFailureReason.MalformedResponse => "MalformedResponse",
        OneCTransportFailureReason.Timeout => "Timeout",
        _ => "SourceUnavailable"
    };

    internal static string OperationMessage(OneCTransportFailureReason reason) => reason switch
    {
        OneCTransportFailureReason.AuthenticationFailed =>
            "1С rejected the configured credentials.",
        OneCTransportFailureReason.EntitySetUnavailable =>
            "A configured 1С entity set is unavailable.",
        OneCTransportFailureReason.MalformedResponse =>
            "The 1С OData service returned an invalid response.",
        OneCTransportFailureReason.Timeout =>
            "The 1С OData request timed out.",
        _ => "The 1С OData service is unavailable."
    };
}
