using Microsoft.Extensions.Options;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.Core.Results;
using Myrmex.Integrations.OneC.Configuration;
using Myrmex.Integrations.OneC.Transport;
using Myrmex.Modules.Wms.Catalog.Features.Imports;
using Myrmex.Modules.Wms.Topology.Features.Imports;
using Myrmex.Shared.Integrations.OneC;

namespace Myrmex.Integrations.OneC.Imports;

internal sealed class OneCImportService(
    IOneCODataClient oDataClient,
    ICommandDispatcher commandDispatcher,
    IOptions<OneCOptions> options,
    OneCImportGate importGate,
    TimeProvider timeProvider) : IOneCImportService
{
    private const int MaximumReturnedErrors = 50;

    public async Task<OneCImportResponse> ImportWarehousesAsync(
        CancellationToken cancellationToken)
    {
        using IDisposable lease = importGate.Acquire(OneCImportGate.Warehouses);
        oDataClient.ValidateConfiguration();
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
            return Incomplete("warehouses", startedAtUtc, OperationReason(exception.Reason), exception.Message);
        }
        catch (Exception)
        {
            return Incomplete("warehouses", startedAtUtc, "BatchCommitFailed",
                "The warehouse import batch could not be committed.");
        }
    }

    public async Task<OneCImportResponse> ImportUnitsOfMeasureAsync(
        CancellationToken cancellationToken)
    {
        using IDisposable lease = importGate.Acquire(OneCImportGate.UnitsOfMeasure);
        oDataClient.ValidateConfiguration();
        DateTimeOffset startedAtUtc = timeProvider.GetUtcNow();

        try
        {
            IReadOnlyList<Catalog_УпаковкиЕдиницыИзмерения> source = await oDataClient
                .ReadUnitsOfMeasureAsync(cancellationToken);
            DateTimeOffset importedAtUtc = timeProvider.GetUtcNow();
            List<ImportUnitsOfMeasure.Item> items = source
                .Select(record => new ImportUnitsOfMeasure.Item(
                    record.Ref_Key,
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
            return Incomplete("uoms", startedAtUtc, OperationReason(exception.Reason), exception.Message);
        }
        catch (Exception)
        {
            return Incomplete("uoms", startedAtUtc, "BatchCommitFailed",
                "The unit-of-measure import batch could not be committed.");
        }
    }

    public async Task<OneCImportResponse> ImportStockKeepingUnitsAsync(
        CancellationToken cancellationToken)
    {
        using IDisposable lease = importGate.Acquire(OneCImportGate.StockKeepingUnits);
        oDataClient.ValidateConfiguration();
        DateTimeOffset startedAtUtc = timeProvider.GetUtcNow();
        int processed = 0;
        int created = 0;
        int updated = 0;
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
                        skipped,
                        failed,
                        errors);
                }

                processed += result.Value.Processed + pendingFolderErrors.Count;
                created += result.Value.Created;
                updated += result.Value.Updated;
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
                skipped,
                failed,
                errors);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Incomplete(
                "skus", startedAtUtc, "Cancelled", "The SKU import was cancelled.",
                processed, created, updated, skipped, failed, errors);
        }
        catch (OneCTransportException exception)
        {
            return Incomplete(
                "skus", startedAtUtc, OperationReason(exception.Reason), exception.Message,
                processed, created, updated, skipped, failed, errors);
        }
        catch (Exception)
        {
            return Incomplete(
                "skus", startedAtUtc, "BatchCommitFailed",
                "The SKU import batch could not be committed.",
                processed, created, updated, skipped, failed, errors);
        }
    }

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
        int skipped,
        int failed,
        IReadOnlyList<OneCImportRecordError> errors) =>
        new(
            ReferenceType: referenceType,
            IsComplete: true,
            Processed: processed,
            Created: created,
            Updated: updated,
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
        int skipped,
        int failed,
        IReadOnlyList<OneCImportRecordError> errors) =>
        new(
            ReferenceType: referenceType,
            IsComplete: false,
            Processed: processed,
            Created: created,
            Updated: updated,
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
}
