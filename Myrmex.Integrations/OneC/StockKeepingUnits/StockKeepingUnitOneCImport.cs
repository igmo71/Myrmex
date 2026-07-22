using Microsoft.Extensions.Logging;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.Core.Results;
using Myrmex.Integrations.OneC.Common.Imports;
using Myrmex.Integrations.OneC.Common.Transport;
using Myrmex.Integrations.OneC.UnitsOfMeasure;
using Myrmex.Modules.Wms.Catalog.Features.Imports;
using Myrmex.Shared.Integrations.OneC;
using System.Diagnostics;

namespace Myrmex.Integrations.OneC.StockKeepingUnits;

internal interface IStockKeepingUnitOneCImport
{
    Task<OneCImportResponse> ImportAsync(CancellationToken cancellationToken);
}

internal sealed class StockKeepingUnitOneCImport(
    IStockKeepingUnitOneCSource source,
    IUnitOfMeasureOneCSource unitOfMeasureSource,
    IOneCODataTransport transport,
    ICommandDispatcher commandDispatcher,
    OneCImportGate importGate,
    OneCImportResponseFactory responseFactory,
    TimeProvider timeProvider,
    ILogger<StockKeepingUnitOneCImport> logger) : IStockKeepingUnitOneCImport
{
    public async Task<OneCImportResponse> ImportAsync(CancellationToken cancellationToken)
    {
        long startedTimestamp = Stopwatch.GetTimestamp();
        try
        {
            using IDisposable lease = importGate.Acquire(OneCImportGate.StockKeepingUnits);
            transport.ValidateConfiguration();
            DateTimeOffset startedAtUtc = timeProvider.GetUtcNow();
            OneCImportResponse response = await ImportStartedAsync(startedAtUtc, cancellationToken);
            LogResult(response, ElapsedMilliseconds(startedTimestamp));
            return response;
        }
        catch (OneCImportAlreadyInProgressException)
        {
            LogRejected("AlreadyInProgress", startedTimestamp);
            throw;
        }
        catch (OneCTransportException exception)
        {
            LogRejected(exception.Reason.ToString(), startedTimestamp);
            throw;
        }
    }

    private async Task<OneCImportResponse> ImportStartedAsync(
        DateTimeOffset startedAtUtc,
        CancellationToken cancellationToken)
    {
        int processed = 0;
        int created = 0;
        int updated = 0;
        int unchanged = 0;
        int skipped = 0;
        int failed = 0;
        List<OneCImportRecordError> errors = [];

        try
        {
            IReadOnlyDictionary<Guid, UnitOfMeasureSourceRecord> units =
                (await unitOfMeasureSource.ReadAllAsync(cancellationToken))
                .ToDictionary(unit => unit.Ref_Key);

            await foreach (IReadOnlyList<StockKeepingUnitSourceRecord> page in
                source.ReadPagesAsync(cancellationToken))
            {
                DateTimeOffset importedAtUtc = timeProvider.GetUtcNow();
                List<OneCImportRecordError> folderErrors = page
                    .Where(record => record.IsFolder)
                    .Select(record => new OneCImportRecordError(
                        record.Ref_Key == Guid.Empty ? null : record.Ref_Key,
                        record.Code?.Trim(),
                        ReferenceImportRecordErrorReasons.SourceFolder,
                        "The 1С nomenclature record is a folder/group and was skipped."))
                    .ToList();
                List<ImportStockKeepingUnits.Item> items = [];
                foreach (StockKeepingUnitSourceRecord record in page.Where(record => !record.IsFolder))
                {
                    StockKeepingUnitPhysicalCharacteristicsNormalizer.Result normalized =
                        StockKeepingUnitPhysicalCharacteristicsNormalizer.Normalize(record, units);
                    LogNormalizationIssues(record.Ref_Key, normalized.Issues);
                    items.Add(new ImportStockKeepingUnits.Item(
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
                        importedAtUtc));
                }

                if (items.Count == 0)
                {
                    processed += folderErrors.Count;
                    skipped += folderErrors.Count;
                    OneCImportResponseFactory.AppendErrors(errors, folderErrors);
                    continue;
                }

                ServiceResult<ReferenceImportBatchResult> result = await commandDispatcher
                    .DispatchAsync<ImportStockKeepingUnits.Command, ServiceResult<ReferenceImportBatchResult>>(
                        new ImportStockKeepingUnits.Command(items),
                        cancellationToken);
                if (!result.IsSuccess)
                {
                    return responseFactory.Incomplete(
                        OneCImportGate.StockKeepingUnits,
                        startedAtUtc,
                        "BatchCommitFailed",
                        "The SKU import batch could not be committed.",
                        processed, created, updated, unchanged, skipped, failed, errors);
                }

                processed += result.Value.Processed + folderErrors.Count;
                created += result.Value.Created;
                updated += result.Value.Updated;
                unchanged += result.Value.Unchanged;
                skipped += result.Value.Skipped + folderErrors.Count;
                failed += result.Value.Failed;
                OneCImportResponseFactory.AppendErrors(errors, folderErrors);
                OneCImportResponseFactory.AppendErrors(
                    errors,
                    OneCImportResponseFactory.ConvertErrors(result.Value.Errors));
            }

            return responseFactory.Complete(
                OneCImportGate.StockKeepingUnits,
                startedAtUtc,
                processed, created, updated, unchanged, skipped, failed, errors);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return responseFactory.Incomplete(
                OneCImportGate.StockKeepingUnits,
                startedAtUtc,
                "Cancelled",
                "The SKU import was cancelled.",
                processed, created, updated, unchanged, skipped, failed, errors);
        }
        catch (OneCTransportException exception)
        {
            return responseFactory.IncompleteFromTransport(
                OneCImportGate.StockKeepingUnits,
                startedAtUtc,
                exception.Reason,
                processed, created, updated, unchanged, skipped, failed, errors);
        }
        catch (Exception)
        {
            return responseFactory.Incomplete(
                OneCImportGate.StockKeepingUnits,
                startedAtUtc,
                "BatchCommitFailed",
                "The SKU import batch could not be committed.",
                processed, created, updated, unchanged, skipped, failed, errors);
        }
    }

    private static string? FirstNonEmpty(string? preferred, string? fallback)
    {
        string? value = string.IsNullOrWhiteSpace(preferred) ? fallback : preferred;
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
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

    private void LogResult(OneCImportResponse response, double durationMilliseconds)
    {
        if (response.IsComplete)
        {
            logger.LogInformation(
                "1С import completed for {ReferenceType} in {DurationMilliseconds} ms. Processed: {Processed}; Created: {Created}; Updated: {Updated}; Unchanged: {Unchanged}; Skipped: {Skipped}; Failed: {Failed}.",
                response.ReferenceType, durationMilliseconds, response.Processed, response.Created,
                response.Updated, response.Unchanged, response.Skipped, response.Failed);
        }
        else
        {
            logger.LogWarning(
                "1С import incomplete for {ReferenceType} in {DurationMilliseconds} ms with category {FailureCategory}. Processed: {Processed}; Created: {Created}; Updated: {Updated}; Unchanged: {Unchanged}; Skipped: {Skipped}; Failed: {Failed}.",
                response.ReferenceType, durationMilliseconds,
                response.OperationError?.Reason ?? "Unknown", response.Processed, response.Created,
                response.Updated, response.Unchanged, response.Skipped, response.Failed);
        }
    }

    private void LogRejected(string category, long startedTimestamp) =>
        logger.LogWarning(
            "1С import rejected for {ReferenceType} in {DurationMilliseconds} ms with category {FailureCategory}.",
            OneCImportGate.StockKeepingUnits,
            ElapsedMilliseconds(startedTimestamp),
            category);

    private static double ElapsedMilliseconds(long startedTimestamp) =>
        Math.Round(Stopwatch.GetElapsedTime(startedTimestamp).TotalMilliseconds, 3);
}
