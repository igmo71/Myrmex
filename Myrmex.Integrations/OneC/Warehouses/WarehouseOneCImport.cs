using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.Core.Results;
using Myrmex.Integrations.OneC.Common.Imports;
using Myrmex.Integrations.OneC.Common.Transport;
using Myrmex.Integrations.OneC.Configuration;
using Myrmex.Modules.Wms.Catalog.Features.Imports;
using Myrmex.Modules.Wms.Topology.Features.Imports;
using Myrmex.Shared.Integrations.OneC;
using System.Diagnostics;

namespace Myrmex.Integrations.OneC.Warehouses;

internal interface IWarehouseOneCImport
{
    Task<OneCImportResponse> ImportAsync(CancellationToken cancellationToken);
}

internal sealed class WarehouseOneCImport(
    IWarehouseOneCSource source,
    IOneCODataTransport transport,
    ICommandDispatcher commandDispatcher,
    IOptions<OneCOptions> options,
    OneCImportGate importGate,
    OneCImportResponseFactory responseFactory,
    TimeProvider timeProvider,
    ILogger<WarehouseOneCImport> logger) : IWarehouseOneCImport
{
    public async Task<OneCImportResponse> ImportAsync(CancellationToken cancellationToken)
    {
        long startedTimestamp = Stopwatch.GetTimestamp();
        try
        {
            using IDisposable lease = importGate.Acquire(OneCImportGate.Warehouses);
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
            IReadOnlyList<WarehouseSourceRecord> records =
                await source.ReadAllAsync(cancellationToken);
            DateTimeOffset importedAtUtc = timeProvider.GetUtcNow();
            List<OneCImportRecordError> folderErrors = records
                .Where(record => record.IsFolder)
                .Select(record => new OneCImportRecordError(
                    record.Ref_Key == Guid.Empty ? null : record.Ref_Key,
                    record.Code?.Trim(),
                    ReferenceImportRecordErrorReasons.SourceFolder,
                    "The 1С warehouse record is a folder/group and was skipped."))
                .ToList();
            List<ImportWarehouses.Item> items = records
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
                return responseFactory.Complete(
                    OneCImportGate.Warehouses,
                    startedAtUtc,
                    folderErrors.Count,
                    0,
                    0,
                    0,
                    folderErrors.Count,
                    0,
                    folderErrors);
            }

            ServiceResult<ReferenceImportBatchResult> result = await commandDispatcher
                .DispatchAsync<ImportWarehouses.Command, ServiceResult<ReferenceImportBatchResult>>(
                    new ImportWarehouses.Command(items),
                    cancellationToken);
            return result.IsSuccess
                ? responseFactory.CompleteFromBatch(
                    OneCImportGate.Warehouses,
                    startedAtUtc,
                    result.Value,
                    folderErrors)
                : responseFactory.Incomplete(
                    OneCImportGate.Warehouses,
                    startedAtUtc,
                    "BatchCommitFailed",
                    "The warehouse import batch could not be committed.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return responseFactory.Incomplete(
                OneCImportGate.Warehouses,
                startedAtUtc,
                "Cancelled",
                "The warehouse import was cancelled.");
        }
        catch (OneCTransportException exception)
        {
            return responseFactory.IncompleteFromTransport(
                OneCImportGate.Warehouses,
                startedAtUtc,
                exception.Reason);
        }
        catch (Exception)
        {
            return responseFactory.Incomplete(
                OneCImportGate.Warehouses,
                startedAtUtc,
                "BatchCommitFailed",
                "The warehouse import batch could not be committed.");
        }
    }

    private string WarehouseCode(WarehouseSourceRecord record)
    {
        string? sourceCode = options.Value.WarehouseCodeAvailable
            ? record.Code?.Trim()
            : null;
        return string.IsNullOrWhiteSpace(sourceCode)
            ? record.Ref_Key.ToString("N").ToUpperInvariant()
            : sourceCode;
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
            OneCImportGate.Warehouses,
            ElapsedMilliseconds(startedTimestamp),
            category);

    private static double ElapsedMilliseconds(long startedTimestamp) =>
        Math.Round(Stopwatch.GetElapsedTime(startedTimestamp).TotalMilliseconds, 3);
}
