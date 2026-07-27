using Microsoft.Extensions.Logging;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Receiving.Features.ReceivingOrders;
using Myrmex.Shared.Integrations.OneC;

namespace Myrmex.Integrations.OneC.ReceivingOrders;

internal interface IReceivingOrderOneCImport
{
    Task<ReceivingOrderImportResponse> ImportAsync(ReceivingOrderImportRequest request, CancellationToken cancellationToken);
}

internal sealed class ReceivingOrderOneCImport(
    IReceivingOrderOneCSource source,
    ICommandDispatcher dispatcher,
    TimeProvider timeProvider,
    ILogger<ReceivingOrderOneCImport> logger) : IReceivingOrderOneCImport
{
    public async Task<ReceivingOrderImportResponse> ImportAsync(ReceivingOrderImportRequest request, CancellationToken cancellationToken)
    {
        DateTimeOffset started = timeProvider.GetUtcNow();
        if (request.StartDate is not DateOnly start || request.EndDate is not DateOnly end || start > end)
        {
            return new(0, 0, 0, 0, 0, started, timeProvider.GetUtcNow(),
                new(ReceivingOrderImportReasons.InvalidPeriod, "Start and end dates are required and must be ordered."), []);
        }

        IReadOnlyList<ReceivingOrderSourceRecord> records;
        try
        {
            records = await source.ReadPeriodAsync(start, end, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "1C receiving-order import could not read period {StartDate} through {EndDate}.", start, end);
            return new(0, 0, 0, 0, 0, started, timeProvider.GetUtcNow(),
                new(ReceivingOrderImportReasons.UnexpectedError, exception.Message), []);
        }

        List<ReceivingOrderImportDocumentResult> results = [];
        int created = 0, updated = 0, skipped = 0, failed = 0;
        HashSet<Guid> seenExternalRefKeys = [];
        foreach (ReceivingOrderSourceRecord record in records)
        {
            if (record.Ref_Key != Guid.Empty && !seenExternalRefKeys.Add(record.Ref_Key))
            {
                failed++;
                logger.LogWarning(
                    "1C receiving-order import skipped duplicate source document {ExternalRefKey}.",
                    record.Ref_Key);
                results.Add(new(
                    record.Ref_Key,
                    record.Number,
                    record.Date,
                    "Failed",
                    ReceivingOrderImportReasons.DuplicateExternalRefKey,
                    "The source period contains the same external receiving-order identity more than once."));
                continue;
            }

            if (!ReceivingOrderOneCMapper.TryMap(record, out ImportExternalReceivingOrder.Document? document, out string reason, out string message))
            {
                failed++;
                results.Add(new(record.Ref_Key == Guid.Empty ? null : record.Ref_Key, record.Number, record.Date, "Failed", reason, message));
                continue;
            }

            try
            {
                ServiceResult<ImportExternalReceivingOrder.Result> result = await dispatcher.DispatchAsync<ImportExternalReceivingOrder.Command, ServiceResult<ImportExternalReceivingOrder.Result>>(
                    new(document!, timeProvider.GetUtcNow()), cancellationToken);
                if (!result.IsSuccess)
                {
                    failed++;
                    results.Add(new(document!.ExternalRefKey, document.Number, document.SourceDate, "Failed", result.Error.Code, result.Error.Message));
                    continue;
                }
                ImportExternalReceivingOrder.Result outcome = result.Value;
                switch (outcome.Outcome) { case "Created": created++; break; case "Updated": updated++; break; case "Skipped": skipped++; break; default: failed++; break; }
                results.Add(new(document!.ExternalRefKey, document.Number, document.SourceDate, outcome.Outcome, outcome.Reason, outcome.Message));
            }
            catch (Exception exception)
            {
                failed++;
                logger.LogWarning(exception, "1C receiving document {ExternalRefKey} failed.", document!.ExternalRefKey);
                results.Add(new(document!.ExternalRefKey, document.Number, document.SourceDate, "Failed", ReceivingOrderImportReasons.UnexpectedError, exception.Message));
            }
        }

        logger.LogInformation("1C receiving-order import completed. Processed {Processed}; Created {Created}; Updated {Updated}; Skipped {Skipped}; Failed {Failed}.", results.Count, created, updated, skipped, failed);
        return new(results.Count, created, updated, skipped, failed, started, timeProvider.GetUtcNow(), null, results);
    }
}
