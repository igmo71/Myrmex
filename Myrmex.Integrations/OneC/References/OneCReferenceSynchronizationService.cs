using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.Core.Results;
using Myrmex.Integrations.OneC.Configuration;
using Myrmex.Integrations.OneC.Imports;
using Myrmex.Integrations.OneC.Transport;
using Myrmex.Modules.Wms.Catalog.Features.Imports;
using Myrmex.Modules.Wms.Topology.Features.Imports;

namespace Myrmex.Integrations.OneC.References;

internal interface IOneCReferenceSynchronizationService
{
    Task<ReferenceSynchronizationResult> SynchronizeWarehouseAsync(
        Guid externalRefKey,
        CancellationToken cancellationToken);

    Task<ReferenceSynchronizationResult> SynchronizeUnitOfMeasureAsync(
        Guid externalRefKey,
        CancellationToken cancellationToken);

    Task<ReferenceSynchronizationResult> SynchronizeStockKeepingUnitAsync(
        Guid externalRefKey,
        CancellationToken cancellationToken);
}

internal sealed class OneCReferenceSynchronizationService(
    IOneCODataClient oDataClient,
    ICommandDispatcher commandDispatcher,
    IOptions<OneCOptions> options,
    OneCImportGate importGate,
    TimeProvider timeProvider,
    ILogger<OneCReferenceSynchronizationService> logger)
    : IOneCReferenceSynchronizationService
{
    public Task<ReferenceSynchronizationResult> SynchronizeWarehouseAsync(
        Guid externalRefKey,
        CancellationToken cancellationToken) =>
        RunAsync(
            OneCReferenceType.Warehouse,
            OneCImportGate.Warehouses,
            externalRefKey,
            SynchronizeWarehouseCoreAsync,
            cancellationToken);

    public Task<ReferenceSynchronizationResult> SynchronizeUnitOfMeasureAsync(
        Guid externalRefKey,
        CancellationToken cancellationToken) =>
        RunAsync(
            OneCReferenceType.UnitOfMeasure,
            OneCImportGate.UnitsOfMeasure,
            externalRefKey,
            SynchronizeUnitOfMeasureCoreAsync,
            cancellationToken);

    public Task<ReferenceSynchronizationResult> SynchronizeStockKeepingUnitAsync(
        Guid externalRefKey,
        CancellationToken cancellationToken) =>
        RunAsync(
            OneCReferenceType.StockKeepingUnit,
            OneCImportGate.StockKeepingUnits,
            externalRefKey,
            SynchronizeStockKeepingUnitCoreAsync,
            cancellationToken);

    private async Task<ReferenceSynchronizationResult> RunAsync(
        OneCReferenceType referenceType,
        string gateType,
        Guid externalRefKey,
        Func<Guid, CancellationToken, Task<ReferenceSynchronizationResult>> synchronize,
        CancellationToken cancellationToken)
    {
        if (externalRefKey == Guid.Empty)
        {
            return PermanentFailure(
                referenceType,
                externalRefKey,
                ReferenceSynchronizationReasons.InvalidRequest,
                "External reference key is required.");
        }

        using IDisposable? lease = importGate.TryAcquire(gateType);
        if (lease is null)
        {
            return ReferenceSynchronizationResult.Failure(
                referenceType,
                externalRefKey,
                ReferenceSynchronizationOutcome.Busy,
                ReferenceSynchronizationReasons.Busy,
                "The reference type is already being synchronized in this application instance.",
                retrySuitable: true);
        }

        try
        {
            ReferenceSynchronizationResult result = await synchronize(externalRefKey, cancellationToken);
            logger.LogInformation(
                "1С current-reference synchronization completed for {ReferenceType} {ExternalRefKey} with outcome {Outcome} and reason {Reason}.",
                referenceType,
                externalRefKey,
                result.Outcome,
                result.Reason);
            return result;
        }
        catch (OneCTransportException exception)
        {
            ReferenceSynchronizationResult result = FromTransportFailure(
                referenceType,
                externalRefKey,
                exception.Reason);
            logger.LogWarning(
                "1С current-reference synchronization failed for {ReferenceType} {ExternalRefKey} with category {FailureCategory}; retry suitable: {RetrySuitable}.",
                referenceType,
                externalRefKey,
                result.Reason,
                result.RetrySuitable);
            return result;
        }
    }

    private async Task<ReferenceSynchronizationResult> SynchronizeWarehouseCoreAsync(
        Guid externalRefKey,
        CancellationToken cancellationToken)
    {
        Catalog_Склады? source = await oDataClient.ReadWarehouseAsync(externalRefKey, cancellationToken);
        if (source is null)
        {
            return NotFound(OneCReferenceType.Warehouse, externalRefKey);
        }

        if (source.IsFolder)
        {
            return ReferenceSynchronizationResult.Success(
                OneCReferenceType.Warehouse,
                externalRefKey,
                ReferenceSynchronizationOutcome.ControlledSkip,
                ReferenceSynchronizationReasons.SourceFolder);
        }

        ImportWarehouses.Item item = new(
            source.Ref_Key,
            source.DataVersion,
            WarehouseCode(source),
            source.Description?.Trim(),
            source.DeletionMark,
            timeProvider.GetUtcNow());
        ServiceResult<ReferenceImportBatchResult> result = await commandDispatcher
            .DispatchAsync<ImportWarehouses.Command, ServiceResult<ReferenceImportBatchResult>>(
                new ImportWarehouses.Command([item]),
                cancellationToken);
        return FromBatchResult(OneCReferenceType.Warehouse, externalRefKey, result);
    }

    private async Task<ReferenceSynchronizationResult> SynchronizeUnitOfMeasureCoreAsync(
        Guid externalRefKey,
        CancellationToken cancellationToken)
    {
        Catalog_УпаковкиЕдиницыИзмерения? source = await oDataClient
            .ReadUnitOfMeasureAsync(externalRefKey, cancellationToken);
        if (source is null)
        {
            return NotFound(OneCReferenceType.UnitOfMeasure, externalRefKey);
        }

        ImportUnitsOfMeasure.Item item = new(
            source.Ref_Key,
            source.DataVersion,
            source.Code?.Trim(),
            FirstNonEmpty(source.НаименованиеПолное, source.Description),
            FirstNonEmpty(source.МеждународноеСокращение, source.Description),
            source.DeletionMark,
            timeProvider.GetUtcNow());
        ServiceResult<ReferenceImportBatchResult> result = await commandDispatcher
            .DispatchAsync<ImportUnitsOfMeasure.Command, ServiceResult<ReferenceImportBatchResult>>(
                new ImportUnitsOfMeasure.Command([item]),
                cancellationToken);
        return FromBatchResult(OneCReferenceType.UnitOfMeasure, externalRefKey, result);
    }

    private async Task<ReferenceSynchronizationResult> SynchronizeStockKeepingUnitCoreAsync(
        Guid externalRefKey,
        CancellationToken cancellationToken)
    {
        Catalog_Номенклатура? source = await oDataClient
            .ReadStockKeepingUnitAsync(externalRefKey, cancellationToken);
        if (source is null)
        {
            return NotFound(OneCReferenceType.StockKeepingUnit, externalRefKey);
        }

        if (source.IsFolder)
        {
            return ReferenceSynchronizationResult.Success(
                OneCReferenceType.StockKeepingUnit,
                externalRefKey,
                ReferenceSynchronizationOutcome.ControlledSkip,
                ReferenceSynchronizationReasons.SourceFolder);
        }

        ImportStockKeepingUnits.Item item = new(
            source.Ref_Key,
            source.DataVersion,
            source.Code?.Trim(),
            FirstNonEmpty(source.НаименованиеПолное, source.Description),
            source.ЕдиницаИзмерения_Key,
            source.DeletionMark,
            timeProvider.GetUtcNow());
        ServiceResult<ReferenceImportBatchResult> result = await commandDispatcher
            .DispatchAsync<ImportStockKeepingUnits.Command, ServiceResult<ReferenceImportBatchResult>>(
                new ImportStockKeepingUnits.Command([item]),
                cancellationToken);
        return FromBatchResult(OneCReferenceType.StockKeepingUnit, externalRefKey, result);
    }

    private string WarehouseCode(Catalog_Склады source)
    {
        string? sourceCode = options.Value.WarehouseCodeAvailable
            ? source.Code?.Trim()
            : null;
        return string.IsNullOrWhiteSpace(sourceCode)
            ? source.Ref_Key.ToString("N").ToUpperInvariant()
            : sourceCode;
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.Select(value => value?.Trim()).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static ReferenceSynchronizationResult FromBatchResult(
        OneCReferenceType referenceType,
        Guid externalRefKey,
        ServiceResult<ReferenceImportBatchResult> result)
    {
        if (!result.IsSuccess)
        {
            bool permanent = result.Error.Type is
                ServiceErrorType.Invalid or
                ServiceErrorType.NotFound or
                ServiceErrorType.Conflict or
                ServiceErrorType.Unauthorized or
                ServiceErrorType.Forbidden;
            return ReferenceSynchronizationResult.Failure(
                referenceType,
                externalRefKey,
                permanent
                    ? ReferenceSynchronizationOutcome.PermanentFailure
                    : ReferenceSynchronizationOutcome.TransientFailure,
                permanent
                    ? ReferenceSynchronizationReasons.BusinessConflict
                    : ReferenceSynchronizationReasons.ApplicationFailure,
                result.Error.Message,
                retrySuitable: !permanent);
        }

        ReferenceImportBatchResult batch = result.Value;
        if (batch.Processed != 1 || !batch.HasConsistentCounts)
        {
            return PermanentFailure(
                referenceType,
                externalRefKey,
                ReferenceSynchronizationReasons.ApplicationFailure,
                "The one-object import returned inconsistent accounting.");
        }

        if (batch.Created + batch.Updated == 1)
        {
            return ReferenceSynchronizationResult.Success(
                referenceType,
                externalRefKey,
                ReferenceSynchronizationOutcome.Applied,
                ReferenceSynchronizationReasons.Applied);
        }

        if (batch.Unchanged == 1)
        {
            return ReferenceSynchronizationResult.Success(
                referenceType,
                externalRefKey,
                ReferenceSynchronizationOutcome.Unchanged,
                ReferenceSynchronizationReasons.Unchanged);
        }

        ReferenceImportRecordError? error = batch.Errors.FirstOrDefault();
        if (batch.Skipped == 1 &&
            error?.Reason == ReferenceImportRecordErrorReasons.SourceRecordDeletionMarked)
        {
            return ReferenceSynchronizationResult.Success(
                referenceType,
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
            referenceType,
            externalRefKey,
            reason,
            error?.Message ?? "The current source object could not be applied.");
    }

    private static ReferenceSynchronizationResult NotFound(
        OneCReferenceType referenceType,
        Guid externalRefKey) =>
        ReferenceSynchronizationResult.Failure(
            referenceType,
            externalRefKey,
            ReferenceSynchronizationOutcome.NotFound,
            ReferenceSynchronizationReasons.NotFound,
            "The current source object was not found.",
            retrySuitable: false);

    private static ReferenceSynchronizationResult PermanentFailure(
        OneCReferenceType referenceType,
        Guid externalRefKey,
        string reason,
        string message) =>
        ReferenceSynchronizationResult.Failure(
            referenceType,
            externalRefKey,
            ReferenceSynchronizationOutcome.PermanentFailure,
            reason,
            message,
            retrySuitable: false);

    private static ReferenceSynchronizationResult FromTransportFailure(
        OneCReferenceType referenceType,
        Guid externalRefKey,
        OneCTransportFailureReason reason) =>
        reason switch
        {
            OneCTransportFailureReason.SourceUnavailable => TransientTransportFailure(
                referenceType, externalRefKey, ReferenceSynchronizationReasons.SourceUnavailable),
            OneCTransportFailureReason.Timeout => TransientTransportFailure(
                referenceType, externalRefKey, ReferenceSynchronizationReasons.Timeout),
            OneCTransportFailureReason.Disabled or OneCTransportFailureReason.InvalidConfiguration => PermanentFailure(
                referenceType, externalRefKey, ReferenceSynchronizationReasons.InvalidConfiguration,
                "The 1С integration configuration is disabled or invalid."),
            OneCTransportFailureReason.AuthenticationFailed => PermanentFailure(
                referenceType, externalRefKey, ReferenceSynchronizationReasons.AuthenticationFailed,
                "1С rejected the configured credentials."),
            OneCTransportFailureReason.EntitySetUnavailable => PermanentFailure(
                referenceType, externalRefKey, ReferenceSynchronizationReasons.EntitySetUnavailable,
                "The configured 1С entity set is unavailable."),
            _ => PermanentFailure(
                referenceType, externalRefKey, ReferenceSynchronizationReasons.MalformedSourceData,
                "The 1С OData service returned malformed source data.")
        };

    private static ReferenceSynchronizationResult TransientTransportFailure(
        OneCReferenceType referenceType,
        Guid externalRefKey,
        string reason) =>
        ReferenceSynchronizationResult.Failure(
            referenceType,
            externalRefKey,
            ReferenceSynchronizationOutcome.TransientFailure,
            reason,
            "The 1С OData service is temporarily unavailable.",
            retrySuitable: true);
}
