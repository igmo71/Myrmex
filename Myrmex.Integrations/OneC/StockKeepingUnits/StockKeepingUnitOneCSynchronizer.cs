using Microsoft.Extensions.Logging;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.Core.Results;
using Myrmex.Integrations.OneC.Common.Imports;
using Myrmex.Integrations.OneC.Common.References;
using Myrmex.Integrations.OneC.Common.Transport;
using Myrmex.Integrations.OneC.UnitsOfMeasure;
using Myrmex.Modules.Wms.Catalog.Features.Imports;

namespace Myrmex.Integrations.OneC.StockKeepingUnits;

internal interface IStockKeepingUnitOneCSynchronizer
{
    Task<ReferenceSynchronizationResult> SynchronizeAsync(
        Guid externalRefKey,
        CancellationToken cancellationToken);
}

internal sealed class StockKeepingUnitOneCSynchronizer(
    IStockKeepingUnitOneCSource source,
    IUnitOfMeasureOneCSource unitOfMeasureSource,
    IUnitOfMeasureOneCSynchronizer unitOfMeasureSynchronizer,
    ICommandDispatcher commandDispatcher,
    OneCImportGate importGate,
    TimeProvider timeProvider,
    ILogger<StockKeepingUnitOneCSynchronizer> logger) : IStockKeepingUnitOneCSynchronizer
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

        using IDisposable? lease = importGate.TryAcquire(OneCImportGate.StockKeepingUnits);
        if (lease is null)
        {
            return ReferenceSynchronizationResult.Failure(
                OneCReferenceType.StockKeepingUnit,
                externalRefKey,
                ReferenceSynchronizationOutcome.Busy,
                ReferenceSynchronizationReasons.Busy,
                "The reference type is already being synchronized in this application instance.",
                retrySuitable: true);
        }

        try
        {
            StockKeepingUnitSourceRecord? record = await source.ReadCurrentAsync(
                externalRefKey,
                cancellationToken);
            ReferenceSynchronizationResult result;
            if (record is null)
            {
                result = NotFound(externalRefKey);
            }
            else if (record.IsFolder)
            {
                result = ReferenceSynchronizationResult.Success(
                    OneCReferenceType.StockKeepingUnit,
                    externalRefKey,
                    ReferenceSynchronizationOutcome.ControlledSkip,
                    ReferenceSynchronizationReasons.SourceFolder);
            }
            else
            {
                IReadOnlyDictionary<Guid, UnitOfMeasureSourceRecord> units =
                    await ReadPhysicalUnitsAsync(record, cancellationToken);
                StockKeepingUnitPhysicalCharacteristicsNormalizer.Result normalized =
                    StockKeepingUnitPhysicalCharacteristicsNormalizer.Normalize(record, units);
                LogNormalizationIssues(record.Ref_Key, normalized.Issues);
                ImportStockKeepingUnits.Item item = new(
                    record.Ref_Key,
                    record.DataVersion,
                    record.Code?.Trim(),
                    FirstNonEmpty(record.НаименованиеПолное, record.Description),
                    record.ЕдиницаИзмерения_Key,
                    normalized.WeightKilograms,
                    normalized.LengthMetres,
                    normalized.AreaSquareMetres,
                    normalized.VolumeCubicMetres,
                    record.DeletionMark,
                    timeProvider.GetUtcNow());
                result = await ApplyWithBoundedRepairAsync(item, cancellationToken);
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

    private async Task<IReadOnlyDictionary<Guid, UnitOfMeasureSourceRecord>> ReadPhysicalUnitsAsync(
        StockKeepingUnitSourceRecord record,
        CancellationToken cancellationToken)
    {
        Guid[] unitExternalRefKeys =
        [
            .. new Guid?[]
            {
                record.ВесИспользовать ? record.ВесЕдиницаИзмерения_Key : null,
                record.ДлинаИспользовать ? record.ДлинаЕдиницаИзмерения_Key : null,
                record.ПлощадьИспользовать ? record.ПлощадьЕдиницаИзмерения_Key : null,
                record.ОбъемИспользовать ? record.ОбъемЕдиницаИзмерения_Key : null
            }
            .Where(key => key.HasValue && key.Value != Guid.Empty)
            .Select(key => key!.Value)
            .Distinct()
        ];

        Dictionary<Guid, UnitOfMeasureSourceRecord> units = [];
        foreach (Guid unitExternalRefKey in unitExternalRefKeys)
        {
            UnitOfMeasureSourceRecord? unit = await unitOfMeasureSource.ReadCurrentAsync(
                unitExternalRefKey,
                cancellationToken);
            if (unit is not null)
            {
                units[unitExternalRefKey] = unit;
            }
        }
        return units;
    }

    private void LogNormalizationIssues(
        Guid stockKeepingUnitExternalRefKey,
        IEnumerable<StockKeepingUnitPhysicalCharacteristicsNormalizer.Issue> issues)
    {
        foreach (StockKeepingUnitPhysicalCharacteristicsNormalizer.Issue issue in issues)
        {
            logger.LogWarning(
                "1С SKU {ExternalRefKey} physical characteristic {Characteristic} could not be normalized: {Reason}. Unit: {UnitExternalRefKey}.",
                stockKeepingUnitExternalRefKey,
                issue.Characteristic,
                issue.Reason,
                issue.UnitExternalRefKey);
        }
    }

    private async Task<ReferenceSynchronizationResult> ApplyWithBoundedRepairAsync(
        ImportStockKeepingUnits.Item item,
        CancellationToken cancellationToken)
    {
        ServiceResult<ReferenceImportBatchResult> first = await DispatchAsync(item, cancellationToken);
        if (!RequiresBaseUnitOfMeasureRepair(first, item.BaseUnitOfMeasureExternalRefKey))
        {
            return FromBatchResult(item.ExternalRefKey, first);
        }

        Guid unitOfMeasureExternalRefKey = item.BaseUnitOfMeasureExternalRefKey!.Value;
        ReferenceSynchronizationResult repair = await unitOfMeasureSynchronizer.SynchronizeAsync(
            unitOfMeasureExternalRefKey,
            cancellationToken);
        if (repair.Outcome is ReferenceSynchronizationOutcome.Busy or
            ReferenceSynchronizationOutcome.TransientFailure)
        {
            return ReferenceSynchronizationResult.Failure(
                OneCReferenceType.StockKeepingUnit,
                item.ExternalRefKey,
                ReferenceSynchronizationOutcome.TransientFailure,
                ReferenceSynchronizationReasons.BaseUnitOfMeasureRepairUnavailable,
                $"Base Unit of Measure {unitOfMeasureExternalRefKey:D} could not be synchronized temporarily.",
                retrySuitable: true);
        }

        if (repair.Outcome is not ReferenceSynchronizationOutcome.Applied and
            not ReferenceSynchronizationOutcome.Unchanged)
        {
            return PermanentFailure(
                item.ExternalRefKey,
                ReferenceSynchronizationReasons.BaseUnitOfMeasureRepairFailed,
                $"Base Unit of Measure {unitOfMeasureExternalRefKey:D} could not be made active and applicable.");
        }

        ServiceResult<ReferenceImportBatchResult> retry = await DispatchAsync(item, cancellationToken);
        if (RequiresBaseUnitOfMeasureRepair(retry, item.BaseUnitOfMeasureExternalRefKey))
        {
            return PermanentFailure(
                item.ExternalRefKey,
                ReferenceSynchronizationReasons.BaseUnitOfMeasureRepairFailed,
                $"Base Unit of Measure {unitOfMeasureExternalRefKey:D} remained missing or inactive after synchronization.");
        }

        return FromBatchResult(item.ExternalRefKey, retry);
    }

    private Task<ServiceResult<ReferenceImportBatchResult>> DispatchAsync(
        ImportStockKeepingUnits.Item item,
        CancellationToken cancellationToken) =>
        commandDispatcher.DispatchAsync<ImportStockKeepingUnits.Command, ServiceResult<ReferenceImportBatchResult>>(
            new ImportStockKeepingUnits.Command([item]),
            cancellationToken);

    private static bool RequiresBaseUnitOfMeasureRepair(
        ServiceResult<ReferenceImportBatchResult> result,
        Guid? unitOfMeasureExternalRefKey) =>
        result.IsSuccess &&
        unitOfMeasureExternalRefKey is Guid key &&
        key != Guid.Empty &&
        result.Value.Processed == 1 &&
        result.Value.Failed == 1 &&
        result.Value.Errors.Any(error => error.Reason is
            ReferenceImportRecordErrorReasons.BaseUnitOfMeasureNotImported or
            ReferenceImportRecordErrorReasons.BaseUnitOfMeasureInactive);

    private static string? FirstNonEmpty(string? preferred, string? fallback)
    {
        string? value = string.IsNullOrWhiteSpace(preferred) ? fallback : preferred;
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

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
                OneCReferenceType.StockKeepingUnit,
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
                OneCReferenceType.StockKeepingUnit,
                externalRefKey,
                ReferenceSynchronizationOutcome.Applied,
                ReferenceSynchronizationReasons.Applied);
        }

        if (batch.Unchanged == 1)
        {
            return ReferenceSynchronizationResult.Success(
                OneCReferenceType.StockKeepingUnit,
                externalRefKey,
                ReferenceSynchronizationOutcome.Unchanged,
                ReferenceSynchronizationReasons.Unchanged);
        }

        ReferenceImportRecordError? error = batch.Errors.FirstOrDefault();
        if (batch.Skipped == 1 &&
            error?.Reason == ReferenceImportRecordErrorReasons.SourceRecordDeletionMarked)
        {
            return ReferenceSynchronizationResult.Success(
                OneCReferenceType.StockKeepingUnit,
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
            OneCReferenceType.StockKeepingUnit,
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
            OneCReferenceType.StockKeepingUnit,
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
            OneCReferenceType.StockKeepingUnit,
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
