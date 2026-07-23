using Microsoft.Extensions.Logging;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.Core.Results;
using Myrmex.Integrations.OneC.Common.Imports;
using Myrmex.Integrations.OneC.Common.References;
using Myrmex.Integrations.OneC.Common.Transport;
using Myrmex.Modules.Wms.Catalog.Features.Imports;

namespace Myrmex.Integrations.OneC.UnitsOfMeasure;

internal interface IUnitOfMeasureOneCSynchronizer
{
    Task<ReferenceSynchronizationResult> SynchronizeAsync(
        Guid externalRefKey,
        CancellationToken cancellationToken);
}

internal sealed class UnitOfMeasureOneCSynchronizer(
    IUnitOfMeasureOneCSource source,
    ICommandDispatcher commandDispatcher,
    OneCImportGate importGate,
    TimeProvider timeProvider,
    ILogger<UnitOfMeasureOneCSynchronizer> logger) : IUnitOfMeasureOneCSynchronizer
{
    public async Task<ReferenceSynchronizationResult> SynchronizeAsync(
        Guid externalRefKey,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (externalRefKey == Guid.Empty)
        {
            return PermanentFailure(
                externalRefKey,
                ReferenceSynchronizationReasons.InvalidRequest,
                "External reference key is required.");
        }

        using IDisposable? lease = importGate.TryAcquire(OneCImportGate.UnitsOfMeasure);
        if (lease is null)
        {
            return ReferenceSynchronizationResult.Failure(
                OneCReferenceType.UnitOfMeasure,
                externalRefKey,
                ReferenceSynchronizationOutcome.Busy,
                ReferenceSynchronizationReasons.Busy,
                "The reference type is already being synchronized in this application instance.",
                retrySuitable: true);
        }

        try
        {
            UnitOfMeasureSourceRecord? record = await source.ReadCurrentAsync(
                externalRefKey,
                cancellationToken);
            ReferenceSynchronizationResult result;
            if (record is null)
            {
                result = NotFound(externalRefKey);
            }
            else
            {
                ImportUnitsOfMeasure.Item item = new(
                    record.Ref_Key,
                    record.DataVersion,
                    TrimToNull(record.МеждународноеСокращение),
                    TrimToNull(record.НаименованиеПолное),
                    TrimToNull(record.Description),
                    record.DeletionMark,
                    timeProvider.GetUtcNow());
                ServiceResult<ReferenceImportBatchResult> batch = await commandDispatcher
                    .DispatchAsync<ImportUnitsOfMeasure.Command, ServiceResult<ReferenceImportBatchResult>>(
                        new ImportUnitsOfMeasure.Command([item]),
                        cancellationToken);
                result = FromBatchResult(externalRefKey, batch);
            }

            LogResult(result);
            return result;
        }
        catch (OneCTransportException exception)
        {
            ReferenceSynchronizationResult result = FromTransportFailure(
                externalRefKey,
                exception.Reason);
            LogResult(result);
            return result;
        }
    }

    private static string? TrimToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ReferenceSynchronizationResult FromBatchResult(
        Guid externalRefKey,
        ServiceResult<ReferenceImportBatchResult> result)
    {
        if (!result.IsSuccess)
        {
            bool permanent = result.Error.Type is
                ServiceErrorType.Invalid or ServiceErrorType.NotFound or
                ServiceErrorType.Conflict or ServiceErrorType.Unauthorized or
                ServiceErrorType.Forbidden;
            return ReferenceSynchronizationResult.Failure(
                OneCReferenceType.UnitOfMeasure,
                externalRefKey,
                permanent ? ReferenceSynchronizationOutcome.PermanentFailure
                    : ReferenceSynchronizationOutcome.TransientFailure,
                permanent ? ReferenceSynchronizationReasons.BusinessConflict
                    : ReferenceSynchronizationReasons.ApplicationFailure,
                result.Error.Message,
                retrySuitable: !permanent);
        }

        ReferenceImportBatchResult batch = result.Value;
        if (batch.Processed != 1 || !batch.HasConsistentCounts)
        {
            return PermanentFailure(
                externalRefKey,
                ReferenceSynchronizationReasons.ApplicationFailure,
                "The one-object import returned inconsistent accounting.");
        }

        if (batch.Created + batch.Updated == 1)
        {
            return ReferenceSynchronizationResult.Success(
                OneCReferenceType.UnitOfMeasure,
                externalRefKey,
                ReferenceSynchronizationOutcome.Applied,
                ReferenceSynchronizationReasons.Applied);
        }

        if (batch.Unchanged == 1)
        {
            return ReferenceSynchronizationResult.Success(
                OneCReferenceType.UnitOfMeasure,
                externalRefKey,
                ReferenceSynchronizationOutcome.Unchanged,
                ReferenceSynchronizationReasons.Unchanged);
        }

        ReferenceImportRecordError? error = batch.Errors.FirstOrDefault();
        if (batch.Skipped == 1 &&
            error?.Reason == ReferenceImportRecordErrorReasons.SourceRecordDeletionMarked)
        {
            return ReferenceSynchronizationResult.Success(
                OneCReferenceType.UnitOfMeasure,
                externalRefKey,
                ReferenceSynchronizationOutcome.ControlledSkip,
                ReferenceSynchronizationReasons.SourceRecordDeletionMarked);
        }

        string reason = error?.Reason is
            ReferenceImportRecordErrorReasons.CodeAlreadyExistsWithoutExternalRefKey or
            ReferenceImportRecordErrorReasons.CodeAlreadyUsedByAnotherRecord
                ? ReferenceSynchronizationReasons.BusinessConflict
                : ReferenceSynchronizationReasons.ValidationFailed;
        return PermanentFailure(
            externalRefKey,
            reason,
            error?.Message ?? "The current source object could not be applied.");
    }

    private static ReferenceSynchronizationResult NotFound(Guid externalRefKey) =>
        ReferenceSynchronizationResult.Failure(
            OneCReferenceType.UnitOfMeasure,
            externalRefKey,
            ReferenceSynchronizationOutcome.NotFound,
            ReferenceSynchronizationReasons.NotFound,
            "The current source object was not found.",
            retrySuitable: false);

    private static ReferenceSynchronizationResult PermanentFailure(
        Guid externalRefKey,
        string reason,
        string message) =>
        ReferenceSynchronizationResult.Failure(
            OneCReferenceType.UnitOfMeasure,
            externalRefKey,
            ReferenceSynchronizationOutcome.PermanentFailure,
            reason,
            message,
            retrySuitable: false);

    private static ReferenceSynchronizationResult FromTransportFailure(
        Guid externalRefKey,
        OneCTransportFailureReason reason) => reason switch
        {
            OneCTransportFailureReason.SourceUnavailable => TransientTransportFailure(
                externalRefKey, ReferenceSynchronizationReasons.SourceUnavailable),
            OneCTransportFailureReason.Timeout => TransientTransportFailure(
                externalRefKey, ReferenceSynchronizationReasons.Timeout),
            OneCTransportFailureReason.Disabled or OneCTransportFailureReason.InvalidConfiguration =>
                PermanentFailure(externalRefKey, ReferenceSynchronizationReasons.InvalidConfiguration,
                    "The 1С integration configuration is disabled or invalid."),
            OneCTransportFailureReason.AuthenticationFailed =>
                PermanentFailure(externalRefKey, ReferenceSynchronizationReasons.AuthenticationFailed,
                    "1С rejected the configured credentials."),
            OneCTransportFailureReason.EntitySetUnavailable =>
                PermanentFailure(externalRefKey, ReferenceSynchronizationReasons.EntitySetUnavailable,
                    "The configured 1С entity set is unavailable."),
            _ => PermanentFailure(externalRefKey, ReferenceSynchronizationReasons.MalformedSourceData,
                "The 1С OData service returned malformed source data.")
        };

    private static ReferenceSynchronizationResult TransientTransportFailure(
        Guid externalRefKey,
        string reason) =>
        ReferenceSynchronizationResult.Failure(
            OneCReferenceType.UnitOfMeasure,
            externalRefKey,
            ReferenceSynchronizationOutcome.TransientFailure,
            reason,
            "The 1С OData service is temporarily unavailable.",
            retrySuitable: true);

    private void LogResult(ReferenceSynchronizationResult result) =>
        logger.LogInformation(
            "1С current-reference synchronization completed for {ReferenceType} {ExternalRefKey} with outcome {Outcome} and reason {Reason}.",
            result.ReferenceType,
            result.ExternalRefKey,
            result.Outcome,
            result.Reason);
}
