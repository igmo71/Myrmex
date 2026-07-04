using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Shared.Wms.DemoData;

namespace Myrmex.Modules.Wms.DemoData.Features;

internal sealed class WmsDemoDataClearService(
    WmsDbContext dbContext,
    TimeProvider timeProvider,
    IWmsDemoDataStageHook stageHook,
    IHostEnvironment hostEnvironment,
    ILogger<WmsDemoDataClearService> logger)
{
    private const string Savepoint = "ClearWmsDemoData";

    public async Task<ServiceResult<DemoDataOperationResponse>> ClearAsync(
        string actorId,
        CancellationToken cancellationToken)
    {
        DateTimeOffset startedAtUtc = timeProvider.GetUtcNow();
        logger.LogInformation(
            "WMS demo data clear attempted by actor {ActorId} in {Environment}.",
            actorId,
            hostEnvironment.EnvironmentName);

        if (!await IsDatabaseReadyAsync(cancellationToken))
        {
            logger.LogWarning(
                "WMS demo data clear rejected for actor {ActorId} in {Environment}; database is not ready.",
                actorId,
                hostEnvironment.EnvironmentName);
            return ServiceResult<DemoDataOperationResponse>.Fail(
                WmsDemoDataErrors.DatabaseNotReady());
        }

        await using WmsDemoDataTransaction transaction =
            await WmsDemoDataTransaction.BeginAsync(dbContext, Savepoint, cancellationToken);

        try
        {
            var deleted = new List<DemoDataAreaSummary>();

            await DeleteAsync("inventoryCountLines", dbContext.InventoryCountLines, deleted, cancellationToken);
            await DeleteAsync("inventoryTransferMovements", dbContext.InventoryTransferMovements, deleted, cancellationToken);
            await DeleteAsync("inventoryTransferLines", dbContext.InventoryTransferLines, deleted, cancellationToken);
            await DeleteAsync("inventoryCounts", dbContext.InventoryCounts, deleted, cancellationToken);
            await DeleteAsync("inventoryTransfers", dbContext.InventoryTransfers, deleted, cancellationToken);
            await stageHook.StageCompletedAsync("clear", "operations", cancellationToken);

            await DeleteAsync("inventoryLedgerEntries", dbContext.InventoryLedgerEntries, deleted, cancellationToken);
            await DeleteAsync("inventoryTransactions", dbContext.InventoryTransactions, deleted, cancellationToken);
            await DeleteAsync("inventoryBalances", dbContext.InventoryBalances, deleted, cancellationToken);
            await stageHook.StageCompletedAsync("clear", "inventory", cancellationToken);

            await DeleteAsync("skuBarcodes", dbContext.SkuBarcodes, deleted, cancellationToken);
            await DeleteAsync("storageLocations", dbContext.StorageLocations, deleted, cancellationToken);
            await DeleteAsync("zones", dbContext.Zones, deleted, cancellationToken);
            await DeleteAsync("stockKeepingUnits", dbContext.StockKeepingUnits, deleted, cancellationToken);
            await DeleteAsync("unitsOfMeasure", dbContext.UnitsOfMeasure, deleted, cancellationToken);
            await DeleteAsync("warehouses", dbContext.Warehouses, deleted, cancellationToken);
            await stageHook.StageCompletedAsync("clear", "catalogAndTopology", cancellationToken);

            DateTimeOffset completedAtUtc = timeProvider.GetUtcNow();
            var response = new DemoDataOperationResponse(
                "clear",
                startedAtUtc,
                completedAtUtc,
                deleted);

            await transaction.CommitAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();

            logger.LogInformation(
                "WMS demo data clear completed for actor {ActorId} in {Environment} after {DurationMilliseconds} ms; deleted {DeletedCount} records.",
                actorId,
                hostEnvironment.EnvironmentName,
                (completedAtUtc - startedAtUtc).TotalMilliseconds,
                deleted.Sum(x => x.Deleted));
            return ServiceResult<DemoDataOperationResponse>.Success(response);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await transaction.RollbackAsync();
            dbContext.ChangeTracker.Clear();
            logger.LogWarning(
                "WMS demo data clear cancelled for actor {ActorId} in {Environment}.",
                actorId,
                hostEnvironment.EnvironmentName);
            throw;
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            dbContext.ChangeTracker.Clear();
            logger.LogError(
                exception,
                "WMS demo data clear failed for actor {ActorId} in {Environment}; transaction rolled back.",
                actorId,
                hostEnvironment.EnvironmentName);
            return ServiceResult<DemoDataOperationResponse>.Fail(
                WmsDemoDataErrors.ExecutionFailed());
        }
    }

    private async Task<bool> IsDatabaseReadyAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!await dbContext.Database.CanConnectAsync(cancellationToken))
            {
                return false;
            }

            IEnumerable<string> pending = await dbContext.Database
                .GetPendingMigrationsAsync(cancellationToken);
            return !pending.Any();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "WMS demo database readiness check failed before clear.");
            return false;
        }
    }

    private static async Task DeleteAsync<TEntity>(
        string area,
        DbSet<TEntity> set,
        ICollection<DemoDataAreaSummary> summaries,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        int count = await set.ExecuteDeleteAsync(cancellationToken);
        summaries.Add(new DemoDataAreaSummary(area, 0, 0, 0, count));
    }
}
