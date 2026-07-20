using Microsoft.Extensions.Logging;
using Myrmex.Integrations.OneC.Common.Transport;
using Myrmex.Integrations.OneC.StockKeepingUnits;
using Myrmex.Integrations.OneC.UnitsOfMeasure;
using Myrmex.Integrations.OneC.Warehouses;
using System.Diagnostics;

namespace Myrmex.Integrations.OneC.Connection;

internal sealed class OneCConnectionTest(
    IOneCODataTransport transport,
    IWarehouseOneCSource warehouseSource,
    IUnitOfMeasureOneCSource unitOfMeasureSource,
    IStockKeepingUnitOneCSource stockKeepingUnitSource,
    ILogger<OneCConnectionTest> logger)
{
    public async Task TestAsync(CancellationToken cancellationToken)
    {
        long startedTimestamp = Stopwatch.GetTimestamp();
        try
        {
            transport.ValidateConfiguration();
            await warehouseSource.ProbeAsync(cancellationToken);
            await unitOfMeasureSource.ProbeAsync(cancellationToken);
            await stockKeepingUnitSource.ProbeAsync(cancellationToken);
            logger.LogInformation(
                "1С connection check completed for {ReferenceType} in {DurationMilliseconds} ms across {CheckedReferenceTypeCount} reference types.",
                "all",
                ElapsedMilliseconds(startedTimestamp),
                3);
        }
        catch (OneCTransportException exception)
        {
            logger.LogWarning(
                "1С connection check failed for {ReferenceType} in {DurationMilliseconds} ms with category {FailureCategory}.",
                "all",
                ElapsedMilliseconds(startedTimestamp),
                exception.Reason.ToString());
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                "1С connection check failed for {ReferenceType} in {DurationMilliseconds} ms with category {FailureCategory}.",
                "all",
                ElapsedMilliseconds(startedTimestamp),
                "Cancelled");
            throw;
        }
        catch (Exception)
        {
            logger.LogWarning(
                "1С connection check failed for {ReferenceType} in {DurationMilliseconds} ms with category {FailureCategory}.",
                "all",
                ElapsedMilliseconds(startedTimestamp),
                "Unexpected");
            throw;
        }
    }

    private static double ElapsedMilliseconds(long startedTimestamp) =>
        Math.Round(Stopwatch.GetElapsedTime(startedTimestamp).TotalMilliseconds, 3);
}
