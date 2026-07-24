using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Receiving.Domain.ReceivingOrders;

namespace Myrmex.Modules.Wms.Receiving.Features.ReceivingOrders;

internal static class ReceivingOrderDraftReconciler
{
    private const string SavepointName = "ReceivingOrderDraftReconciliation";

    public static DomainValidationResult Replace(
        ReceivingOrder order,
        string? number,
        Guid? warehouseId,
        Guid? receivingLocationId,
        IEnumerable<ReceivingOrder.DraftLine> lines,
        out IReadOnlyList<ReceivingOrderLine> removedLines) =>
        order.ReplaceDraft(number, warehouseId, receivingLocationId, lines, out removedLines);

    public static async Task<ServiceError?> PersistAsync(
        WmsDbContext dbContext,
        ReceivingOrder order,
        IReadOnlyDictionary<Guid, Guid> persistedSkuByLineId,
        IReadOnlyList<ReceivingOrderLine> removedLines,
        string concurrencyProperty,
        CancellationToken cancellationToken)
    {
        ReceivingOrderLine[] reassignedLines = [.. order.Lines.Where(line =>
            persistedSkuByLineId.TryGetValue(line.Id, out Guid persistedSkuId) &&
            persistedSkuId != line.StockKeepingUnitId)];
        ReceivingOrderLine[] newLines = [.. order.Lines.Where(line => !persistedSkuByLineId.ContainsKey(line.Id))];
        Guid[] releasedLineIds = [.. removedLines.Concat(reassignedLines).Select(line => line.Id).Distinct()];
        IDbContextTransaction? ownedTransaction = null;
        IDbContextTransaction? transaction = null;
        try
        {
            if (releasedLineIds.Length > 0)
            {
                if (dbContext.Database.CurrentTransaction is null)
                {
                    ownedTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                    transaction = ownedTransaction;
                }
                else
                {
                    transaction = dbContext.Database.CurrentTransaction!;
                    await transaction.CreateSavepointAsync(SavepointName, cancellationToken);
                }
                foreach (ReceivingOrderLine line in removedLines.Concat(reassignedLines))
                {
                    dbContext.Entry(line).State = EntityState.Detached;
                }
                int released = await dbContext.ReceivingOrderLines.Where(line =>
                    line.ReceivingOrderId == order.Id && releasedLineIds.Contains(line.Id)).ExecuteDeleteAsync(cancellationToken);
                if (released != releasedLineIds.Length)
                {
                    await RollbackAsync(transaction, ownedTransaction is not null);
                    dbContext.ChangeTracker.Clear();
                    return ReceivingOrderErrors.ConcurrencyConflict(concurrencyProperty);
                }
                dbContext.ReceivingOrderLines.AddRange(reassignedLines);
            }
            else
            {
                dbContext.ReceivingOrderLines.RemoveRange(removedLines);
            }
            dbContext.ReceivingOrderLines.AddRange(newLines);
            await dbContext.SaveChangesAsync(cancellationToken);
            if (ownedTransaction is not null) await transaction!.CommitAsync(cancellationToken);
            else if (transaction is not null) await transaction.ReleaseSavepointAsync(SavepointName, cancellationToken);
            return null;
        }
        catch (DbUpdateConcurrencyException)
        {
            await RollbackAsync(transaction, ownedTransaction is not null);
            dbContext.ChangeTracker.Clear();
            return ReceivingOrderErrors.ConcurrencyConflict(concurrencyProperty);
        }
        catch (DbUpdateException exception)
        {
            await RollbackAsync(transaction, ownedTransaction is not null);
            dbContext.ChangeTracker.Clear();
            ServiceError? error = WmsPersistenceExceptionMapper.TryMap(exception);
            if (error is not null)
            {
                return error;
            }

            throw;
        }
        catch
        {
            await RollbackAsync(transaction, ownedTransaction is not null);
            dbContext.ChangeTracker.Clear();
            throw;
        }
        finally
        {
            if (ownedTransaction is not null) await ownedTransaction.DisposeAsync();
        }
    }

    private static Task RollbackAsync(IDbContextTransaction? transaction, bool ownsTransaction) =>
        transaction is null ? Task.CompletedTask : ownsTransaction
            ? transaction.RollbackAsync(CancellationToken.None)
            : transaction.RollbackToSavepointAsync(SavepointName, CancellationToken.None);
}
