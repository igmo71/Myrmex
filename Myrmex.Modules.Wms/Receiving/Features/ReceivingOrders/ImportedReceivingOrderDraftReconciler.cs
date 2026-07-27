using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Receiving.Domain.ReceivingOrders;

namespace Myrmex.Modules.Wms.Receiving.Features.ReceivingOrders;

internal static class ImportedReceivingOrderDraftReconciler
{
    private const string SavepointName = "ImportedReceivingOrderDraftReconciliation";

    public static async Task<ServiceError?> PersistAsync(
        WmsDbContext dbContext,
        ReceivingOrder order,
        IReadOnlyList<ReceivingOrderLine> removedLines,
        string concurrencyProperty,
        CancellationToken cancellationToken)
    {
        IDbContextTransaction? ownedTransaction = null;
        IDbContextTransaction? transaction = null;

        try
        {
            if (removedLines.Count > 0)
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

                Guid[] removedLineIds = [.. removedLines.Select(line => line.Id)];
                foreach (ReceivingOrderLine removedLine in removedLines)
                {
                    dbContext.Entry(removedLine).State = EntityState.Detached;
                }

                int deleted = await dbContext.ReceivingOrderLines
                    .Where(line => line.ReceivingOrderId == order.Id && removedLineIds.Contains(line.Id))
                    .ExecuteDeleteAsync(cancellationToken);

                if (deleted != removedLineIds.Length)
                {
                    await RollbackAsync(transaction, ownedTransaction is not null);
                    dbContext.ChangeTracker.Clear();
                    return ReceivingOrderErrors.ConcurrencyConflict(concurrencyProperty);
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            if (ownedTransaction is not null)
                await transaction!.CommitAsync(cancellationToken);
            else if (transaction is not null)
                await transaction.ReleaseSavepointAsync(SavepointName, cancellationToken);

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
                return error;

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
            if (ownedTransaction is not null)
                await ownedTransaction.DisposeAsync();
        }
    }

    private static Task RollbackAsync(IDbContextTransaction? transaction, bool ownsTransaction) =>
        transaction is null ? Task.CompletedTask : ownsTransaction
            ? transaction.RollbackAsync(CancellationToken.None)
            : transaction.RollbackToSavepointAsync(SavepointName, CancellationToken.None);
}
