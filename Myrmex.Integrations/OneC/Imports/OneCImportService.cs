using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.Core.Results;
using Myrmex.Integrations.OneC.Configuration;
using Myrmex.Integrations.OneC.Transport;
using Myrmex.Modules.Wms.Catalog.Features.Imports;
using Myrmex.Modules.Wms.Topology.Features.Imports;
using Myrmex.Shared.Integrations.OneC;
using System.Diagnostics;

namespace Myrmex.Integrations.OneC.Imports;

internal sealed class OneCImportService(
    IOneCODataClient oDataClient,
    ICommandDispatcher commandDispatcher,
    IOptions<OneCOptions> options,
    OneCImportGate importGate,
    TimeProvider timeProvider,
    ILogger<OneCImportService> logger) : IOneCImportService
{
    private const int MaximumReturnedErrors = 50;

    public Task<OneCImportResponse> ImportWarehousesAsync(CancellationToken cancellationToken) =>
        RunImportAsync(
            OneCImportGate.Warehouses,
            ImportWarehousesCoreAsync,
            cancellationToken);

    private async Task<OneCImportResponse> ImportWarehousesCoreAsync(
        CancellationToken cancellationToken)
    {
        DateTimeOffset startedAtUtc = timeProvider.GetUtcNow();

        try
        {
            IReadOnlyList<Catalog_Склады> source = await oDataClient
                .ReadWarehousesAsync(cancellationToken);
            DateTimeOffset importedAtUtc = timeProvider.GetUtcNow();
            List<OneCImportRecordError> pendingFolderErrors = source
                .Where(record => record.IsFolder)
                .Select(record => new OneCImportRecordError(
                    record.Ref_Key == Guid.Empty ? null : record.Ref_Key,
                    record.Code?.Trim(),
                    ReferenceImportRecordErrorReasons.SourceFolder,
                    "The 1С warehouse record is a folder/group and was skipped."))
                .ToList();
            List<ImportWarehouses.Item> items = source
                .Where(record => !record.IsFolder)
                .Select(record => new ImportWarehouses.Item(
                    record.Ref_Key,
                    record.DataVersion,
                    WarehouseCode(record),
                    record.Description?.Trim(),
                    record.DeletionMark,
                    importedAtUtc))
                .ToList();

            if (items.Count == 0)
            {
                return Complete(
                    "warehouses",
                    startedAtUtc,
                    processed: pendingFolderErrors.Count,
                    created: 0,
                    updated: 0,
                    unchanged: 0,
                    skipped: pendingFolderErrors.Count,
                    failed: 0,
                    pendingFolderErrors);
            }

            ServiceResult<ReferenceImportBatchResult> result = await commandDispatcher
                .DispatchAsync<ImportWarehouses.Command, ServiceResult<ReferenceImportBatchResult>>(
                    new ImportWarehouses.Command(items),
                    cancellationToken);

            if (!result.IsSuccess)
            {
                return Incomplete("warehouses", startedAtUtc, "BatchCommitFailed",
                    "The warehouse import batch could not be committed.");
            }

            return CompleteFromBatch(
                "warehouses",
                startedAtUtc,
                result.Value,
                pendingFolderErrors);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Incomplete("warehouses", startedAtUtc, "Cancelled",
                "The warehouse import was cancelled.");
        }
        catch (OneCTransportException exception)
        {
            return Incomplete(
                "warehouses",
                startedAtUtc,
                OperationReason(exception.Reason),
                OperationMessage(exception.Reason));
        }
        catch (Exception)
        {
            return Incomplete("warehouses", startedAtUtc, "BatchCommitFailed",
                "The warehouse import batch could not be committed.");
        }
    }

    public Task<OneCImportResponse> ImportUnitsOfMeasureAsync(CancellationToken cancellationToken) =>
        RunImportAsync(
            OneCImportGate.UnitsOfMeasure,
            ImportUnitsOfMeasureCoreAsync,
            cancellationToken);

    private async Task<OneCImportResponse> ImportUnitsOfMeasureCoreAsync(
        CancellationToken cancellationToken)
    {
        DateTimeOffset startedAtUtc = timeProvider.GetUtcNow();

        try
        {
            IReadOnlyList<Catalog_УпаковкиЕдиницыИзмерения> source = await oDataClient
                .ReadUnitsOfMeasureAsync(cancellationToken);
            DateTimeOffset importedAtUtc = timeProvider.GetUtcNow();
            List<ImportUnitsOfMeasure.Item> items = source
                .Select(record => new ImportUnitsOfMeasure.Item(
                    record.Ref_Key,
                    record.DataVersion,
                    record.Code?.Trim(),
                    FirstNonEmpty(record.НаименованиеПолное, record.Description),
                    FirstNonEmpty(record.МеждународноеСокращение, record.Description),
                    record.DeletionMark,
                    importedAtUtc))
                .ToList();

            if (items.Count == 0)
            {
                return Complete(
                    "uoms",
                    startedAtUtc,
                    processed: 0,
                    created: 0,
                    updated: 0,
                    unchanged: 0,
                    skipped: 0,
                    failed: 0,
                    []);
            }

            ServiceResult<ReferenceImportBatchResult> result = await commandDispatcher
                .DispatchAsync<ImportUnitsOfMeasure.Command, ServiceResult<ReferenceImportBatchResult>>(
                    new ImportUnitsOfMeasure.Command(items),
                    cancellationToken);

            if (!result.IsSuccess)
            {
                return Incomplete("uoms", startedAtUtc, "BatchCommitFailed",
                    "The unit-of-measure import batch could not be committed.");
            }

            return CompleteFromBatch("uoms", startedAtUtc, result.Value, []);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Incomplete("uoms", startedAtUtc, "Cancelled",
                "The unit-of-measure import was cancelled.");
        }
        catch (OneCTransportException exception)
        {
            return Incomplete(
                "uoms",
                startedAtUtc,
                OperationReason(exception.Reason),
                OperationMessage(exception.Reason));
        }
        catch (Exception)
        {
            return Incomplete("uoms", startedAtUtc, "BatchCommitFailed",
                "The unit-of-measure import batch could not be committed.");
        }
    }

    public Task<OneCImportResponse> ImportStockKeepingUnitsAsync(CancellationToken cancellationToken) =>
        RunImportAsync(
            OneCImportGate.StockKeepingUnits,
            ImportStockKeepingUnitsCoreAsync,
            cancellationToken);

    private async Task<OneCImportResponse> ImportStockKeepingUnitsCoreAsync(
        CancellationToken cancellationToken)
    {
        DateTimeOffset startedAtUtc = timeProvider.GetUtcNow();
        int processed = 0;
        int created = 0;
        int updated = 0;
        int unchanged = 0;
        int skipped = 0;
        int failed = 0;
        List<OneCImportRecordError> errors = [];

        try
        {
            await foreach (IReadOnlyList<Catalog_Номенклатура> sourcePage in
                oDataClient.ReadNomenclaturePagesAsync(cancellationToken))
            {
                DateTimeOffset importedAtUtc = timeProvider.GetUtcNow();
                List<OneCImportRecordError> pendingFolderErrors = sourcePage
                    .Where(record => record.IsFolder)
                    .Select(record => new OneCImportRecordError(
                        record.Ref_Key == Guid.Empty ? null : record.Ref_Key,
                        record.Code?.Trim(),
                        ReferenceImportRecordErrorReasons.SourceFolder,
                        "The 1С nomenclature record is a folder/group and was skipped."))
                    .ToList();
                List<ImportStockKeepingUnits.Item> items = sourcePage
                    .Where(record => !record.IsFolder)
                    .Select(record => new ImportStockKeepingUnits.Item(
                        record.Ref_Key,
                        record.DataVersion,
                        record.Code?.Trim(),
                        FirstNonEmpty(record.НаименованиеПолное, record.Description),
                        record.ЕдиницаИзмерения_Key,
                        record.DeletionMark,
                        importedAtUtc))
                    .ToList();

                if (items.Count == 0)
                {
                    processed += pendingFolderErrors.Count;
                    skipped += pendingFolderErrors.Count;
                    AppendErrors(errors, pendingFolderErrors);
                    continue;
                }

                ServiceResult<ReferenceImportBatchResult> result = await commandDispatcher
                    .DispatchAsync<ImportStockKeepingUnits.Command, ServiceResult<ReferenceImportBatchResult>>(
                        new ImportStockKeepingUnits.Command(items),
                        cancellationToken);

                if (!result.IsSuccess)
                {
                    return Incomplete(
                        "skus",
                        startedAtUtc,
                        "BatchCommitFailed",
                        "The SKU import batch could not be committed.",
                        processed,
                        created,
                        updated,
                        unchanged,
                        skipped,
                        failed,
                        errors);
                }

                processed += result.Value.Processed + pendingFolderErrors.Count;
                created += result.Value.Created;
                updated += result.Value.Updated;
                unchanged += result.Value.Unchanged;
                skipped += result.Value.Skipped + pendingFolderErrors.Count;
                failed += result.Value.Failed;
                AppendErrors(errors, pendingFolderErrors);
                AppendErrors(errors, result.Value.Errors.Select(error => new OneCImportRecordError(
                    error.ExternalRefKey,
                    error.Code,
                    error.Reason,
                    error.Message)));
            }

            return Complete(
                "skus",
                startedAtUtc,
                processed,
                created,
                updated,
                unchanged,
                skipped,
                failed,
                errors);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Incomplete(
                "skus", startedAtUtc, "Cancelled", "The SKU import was cancelled.",
                processed, created, updated, unchanged, skipped, failed, errors);
        }
        catch (OneCTransportException exception)
        {
            return Incomplete(
                "skus",
                startedAtUtc,
                OperationReason(exception.Reason),
                OperationMessage(exception.Reason),
                processed, created, updated, unchanged, skipped, failed, errors);
        }
        catch (Exception)
        {
            return Incomplete(
                "skus", startedAtUtc, "BatchCommitFailed",
                "The SKU import batch could not be committed.",
                processed, created, updated, unchanged, skipped, failed, errors);
        }
    }

    private async Task<OneCImportResponse> RunImportAsync(
        string referenceType,
        Func<CancellationToken, Task<OneCImportResponse>> import,
        CancellationToken cancellationToken)
    {
        long startedTimestamp = Stopwatch.GetTimestamp();
        try
        {
            using IDisposable lease = importGate.Acquire(referenceType);
            oDataClient.ValidateConfiguration();
            OneCImportResponse response = await import(cancellationToken);
            LogImportResult(response, ElapsedMilliseconds(startedTimestamp));
            return response;
        }
        catch (OneCImportAlreadyInProgressException)
        {
            LogRejectedImport(referenceType, "AlreadyInProgress", startedTimestamp);
            throw;
        }
        catch (OneCTransportException exception)
        {
            LogRejectedImport(referenceType, exception.Reason.ToString(), startedTimestamp);
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            LogRejectedImport(referenceType, "Cancelled", startedTimestamp);
            throw;
        }
        catch (Exception)
        {
            LogRejectedImport(referenceType, "Unexpected", startedTimestamp);
            throw;
        }
    }

    private void LogImportResult(OneCImportResponse response, double durationMilliseconds)
    {
        if (response.IsComplete)
        {
            logger.LogInformation(
                "1С import completed for {ReferenceType} in {DurationMilliseconds} ms. Processed: {Processed}; Created: {Created}; Updated: {Updated}; Unchanged: {Unchanged}; Skipped: {Skipped}; Failed: {Failed}.",
                response.ReferenceType,
                durationMilliseconds,
                response.Processed,
                response.Created,
                response.Updated,
                response.Unchanged,
                response.Skipped,
                response.Failed);
            return;
        }

        logger.LogWarning(
            "1С import incomplete for {ReferenceType} in {DurationMilliseconds} ms with category {FailureCategory}. Processed: {Processed}; Created: {Created}; Updated: {Updated}; Unchanged: {Unchanged}; Skipped: {Skipped}; Failed: {Failed}.",
            response.ReferenceType,
            durationMilliseconds,
            response.OperationError?.Reason ?? "Unknown",
            response.Processed,
            response.Created,
            response.Updated,
            response.Unchanged,
            response.Skipped,
            response.Failed);
    }

    private void LogRejectedImport(
        string referenceType,
        string failureCategory,
        long startedTimestamp) =>
        logger.LogWarning(
            "1С import rejected for {ReferenceType} in {DurationMilliseconds} ms with category {FailureCategory}.",
            referenceType,
            ElapsedMilliseconds(startedTimestamp),
            failureCategory);

    private string WarehouseCode(Catalog_Склады record)
    {
        string? sourceCode = options.Value.WarehouseCodeAvailable
            ? record.Code?.Trim()
            : null;
        return string.IsNullOrWhiteSpace(sourceCode)
            ? record.Ref_Key.ToString("N").ToUpperInvariant()
            : sourceCode;
    }

    private OneCImportResponse CompleteFromBatch(
        string referenceType,
        DateTimeOffset startedAtUtc,
        ReferenceImportBatchResult batch,
        IReadOnlyList<OneCImportRecordError> pendingErrors)
    {
        List<OneCImportRecordError> errors = pendingErrors
            .Concat(batch.Errors.Select(error => new OneCImportRecordError(
                error.ExternalRefKey,
                error.Code,
                error.Reason,
                error.Message)))
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

    private OneCImportResponse Complete(
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

    private OneCImportResponse Incomplete(
        string referenceType,
        DateTimeOffset startedAtUtc,
        string reason,
        string message) =>
        new(
            ReferenceType: referenceType,
            IsComplete: false,
            Processed: 0,
            Created: 0,
            Updated: 0,
            Unchanged: 0,
            Skipped: 0,
            Failed: 0,
            StartedAtUtc: startedAtUtc,
            CompletedAtUtc: timeProvider.GetUtcNow(),
            OperationError: new OneCImportOperationError(reason, message),
            Errors: []);

    private OneCImportResponse Incomplete(
        string referenceType,
        DateTimeOffset startedAtUtc,
        string reason,
        string message,
        int processed,
        int created,
        int updated,
        int unchanged,
        int skipped,
        int failed,
        IReadOnlyList<OneCImportRecordError> errors) =>
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
            Errors: errors.Take(MaximumReturnedErrors).ToArray());

    private static void AppendErrors(
        ICollection<OneCImportRecordError> target,
        IEnumerable<OneCImportRecordError> source)
    {
        foreach (OneCImportRecordError error in source.Take(MaximumReturnedErrors - target.Count))
        {
            target.Add(error);
        }
    }

    private static string? FirstNonEmpty(string? preferred, string? fallback)
    {
        string? value = string.IsNullOrWhiteSpace(preferred) ? fallback : preferred;
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string OperationReason(OneCTransportFailureReason reason) => reason switch
    {
        OneCTransportFailureReason.AuthenticationFailed => "AuthenticationFailed",
        OneCTransportFailureReason.EntitySetUnavailable => "EntitySetUnavailable",
        OneCTransportFailureReason.MalformedResponse => "MalformedResponse",
        OneCTransportFailureReason.Timeout => "Timeout",
        _ => "SourceUnavailable"
    };

    private static string OperationMessage(OneCTransportFailureReason reason) => reason switch
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

    private static double ElapsedMilliseconds(long startedTimestamp) =>
        Math.Round(Stopwatch.GetElapsedTime(startedTimestamp).TotalMilliseconds, 3);
}
