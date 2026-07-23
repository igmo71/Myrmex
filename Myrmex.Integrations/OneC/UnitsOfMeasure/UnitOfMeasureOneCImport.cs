using Microsoft.Extensions.Logging;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.Core.Results;
using Myrmex.Integrations.OneC.Common.Imports;
using Myrmex.Integrations.OneC.Common.Transport;
using Myrmex.Modules.Wms.Catalog.Features.Imports;
using Myrmex.Shared.Integrations.OneC;
using System.Diagnostics;

namespace Myrmex.Integrations.OneC.UnitsOfMeasure;

internal interface IUnitOfMeasureOneCImport
{
    Task<OneCImportResponse> ImportAsync(CancellationToken cancellationToken);
}

internal sealed class UnitOfMeasureOneCImport(
    IUnitOfMeasureOneCSource source,
    IOneCODataTransport transport,
    ICommandDispatcher commandDispatcher,
    OneCImportGate importGate,
    OneCImportResponseFactory responseFactory,
    TimeProvider timeProvider,
    ILogger<UnitOfMeasureOneCImport> logger) : IUnitOfMeasureOneCImport
{
    public async Task<OneCImportResponse> ImportAsync(CancellationToken cancellationToken)
    {
        long startedTimestamp = Stopwatch.GetTimestamp();
        try
        {
            using IDisposable lease = importGate.Acquire(OneCImportGate.UnitsOfMeasure);
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
        try
        {
            IReadOnlyList<UnitOfMeasureSourceRecord> records =
                await source.ReadAllAsync(cancellationToken);
            DateTimeOffset importedAtUtc = timeProvider.GetUtcNow();
            List<ImportUnitsOfMeasure.Item> items = records
                .Select(record => new ImportUnitsOfMeasure.Item(
                    record.Ref_Key,
                    record.DataVersion,
                    TrimToNull(record.МеждународноеСокращение),
                    TrimToNull(record.НаименованиеПолное),
                    TrimToNull(record.Description),
                    record.DeletionMark,
                    importedAtUtc))
                .ToList();

            if (items.Count == 0)
            {
                return responseFactory.Complete(
                    OneCImportGate.UnitsOfMeasure,
                    startedAtUtc,
                    0, 0, 0, 0, 0, 0, []);
            }

            ServiceResult<ReferenceImportBatchResult> result = await commandDispatcher
                .DispatchAsync<ImportUnitsOfMeasure.Command, ServiceResult<ReferenceImportBatchResult>>(
                    new ImportUnitsOfMeasure.Command(items),
                    cancellationToken);
            return result.IsSuccess
                ? responseFactory.CompleteFromBatch(
                    OneCImportGate.UnitsOfMeasure,
                    startedAtUtc,
                    result.Value,
                    [])
                : responseFactory.Incomplete(
                    OneCImportGate.UnitsOfMeasure,
                    startedAtUtc,
                    "BatchCommitFailed",
                    "The unit-of-measure import batch could not be committed.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return responseFactory.Incomplete(
                OneCImportGate.UnitsOfMeasure,
                startedAtUtc,
                "Cancelled",
                "The unit-of-measure import was cancelled.");
        }
        catch (OneCTransportException exception)
        {
            return responseFactory.IncompleteFromTransport(
                OneCImportGate.UnitsOfMeasure,
                startedAtUtc,
                exception.Reason);
        }
        catch (Exception)
        {
            return responseFactory.Incomplete(
                OneCImportGate.UnitsOfMeasure,
                startedAtUtc,
                "BatchCommitFailed",
                "The unit-of-measure import batch could not be committed.");
        }
    }

    private static string? TrimToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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
            OneCImportGate.UnitsOfMeasure,
            ElapsedMilliseconds(startedTimestamp),
            category);

    private static double ElapsedMilliseconds(long startedTimestamp) =>
        Math.Round(Stopwatch.GetElapsedTime(startedTimestamp).TotalMilliseconds, 3);
}
