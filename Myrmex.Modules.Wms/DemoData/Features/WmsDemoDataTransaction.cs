using Microsoft.EntityFrameworkCore.Storage;
using Myrmex.Modules.Wms.Infrastructure.Persistence;

namespace Myrmex.Modules.Wms.DemoData.Features;

internal sealed class WmsDemoDataTransaction : IAsyncDisposable
{
    private readonly IDbContextTransaction _transaction;
    private readonly bool _ownsTransaction;
    private readonly string _savepoint;

    private WmsDemoDataTransaction(
        IDbContextTransaction transaction,
        bool ownsTransaction,
        string savepoint)
    {
        _transaction = transaction;
        _ownsTransaction = ownsTransaction;
        _savepoint = savepoint;
    }

    public static async Task<WmsDemoDataTransaction> BeginAsync(
        WmsDbContext dbContext,
        string savepoint,
        CancellationToken cancellationToken)
    {
        if (dbContext.Database.CurrentTransaction is null)
        {
            IDbContextTransaction transaction =
                await dbContext.Database.BeginTransactionAsync(cancellationToken);
            return new WmsDemoDataTransaction(transaction, true, savepoint);
        }

        IDbContextTransaction current = dbContext.Database.CurrentTransaction;
        await current.CreateSavepointAsync(savepoint, cancellationToken);
        return new WmsDemoDataTransaction(current, false, savepoint);
    }

    public Task CommitAsync(CancellationToken cancellationToken) =>
        _ownsTransaction
            ? _transaction.CommitAsync(cancellationToken)
            : _transaction.ReleaseSavepointAsync(_savepoint, cancellationToken);

    public Task RollbackAsync() =>
        _ownsTransaction
            ? _transaction.RollbackAsync(CancellationToken.None)
            : _transaction.RollbackToSavepointAsync(_savepoint, CancellationToken.None);

    public async ValueTask DisposeAsync()
    {
        if (_ownsTransaction)
        {
            await _transaction.DisposeAsync();
        }
    }
}
