using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Myrmex.Core.Application;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Receiving.Domain.ReceivingOrders;
using Myrmex.Shared.Wms.Receiving;

namespace Myrmex.Modules.Wms.Receiving.Features.ReceivingOrders;

internal static class UpdateReceivingOrderDraft
{
    private const string SkuReconciliationSavepoint = "UpdateReceivingOrderDraftSkuReconciliation";

    internal sealed record Command(
        Guid? ReceivingOrderId,
        string? Number,
        Guid? WarehouseId,
        Guid? ReceivingLocationId,
        string? ExpectedOrderVersion,
        IReadOnlyList<UpdateReceivingOrderLineRequest>? Lines,
        string? ActorId) : ICommand<ServiceResult<ReceivingOrderDetails>>;

    internal sealed class Handler(WmsDbContext dbContext, ILogger<Handler> logger)
        : ICommandHandler<Command, ServiceResult<ReceivingOrderDetails>>
    {
        public async Task<ServiceResult<ReceivingOrderDetails>> HandleAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            List<DomainValidationFailure> shapeErrors = ValidateShape(command, out byte[]? expectedVersion);
            if (shapeErrors.Count > 0)
            {
                return ServiceResult<ReceivingOrderDetails>.Invalid(shapeErrors);
            }

            ReceivingOrder? order = await dbContext.ReceivingOrders
                .Include(x => x.Lines)
                .SingleOrDefaultAsync(x => x.Id == command.ReceivingOrderId!.Value, cancellationToken);
            if (order is null)
            {
                return ServiceResult<ReceivingOrderDetails>.Fail(
                    ServiceError.NotFound<ReceivingOrder>(
                        "ReceivingOrder not found",
                        nameof(Command.ReceivingOrderId)));
            }

            if (order.Status != ReceivingOrderStatus.Draft)
            {
                return ServiceResult<ReceivingOrderDetails>.Fail(
                    ReceivingOrderErrors.InvalidState(
                        "Only Draft receiving orders may be revised."));
            }

            if (!order.RowVersion.SequenceEqual(expectedVersion!))
            {
                return ServiceResult<ReceivingOrderDetails>.Fail(
                    ReceivingOrderErrors.ConcurrencyConflict(nameof(Command.ExpectedOrderVersion)));
            }

            Dictionary<Guid, Guid> persistedSkuByLineId = order.Lines
                .ToDictionary(line => line.Id, line => line.StockKeepingUnitId);
            UpdateReceivingOrderLineRequest[] requestLines = command.Lines?.ToArray() ?? [];
            DomainValidationResult replacement = ReceivingOrderDraftReconciler.Replace(
                command.Number,
                command.WarehouseId,
                command.ReceivingLocationId,
                requestLines.Select(line => new ReceivingOrder.DraftLine(
                    line.LineId,
                    line.StockKeepingUnitId,
                    line.PlannedQuantity)),
                out IReadOnlyList<ReceivingOrderLine> removedLines);
            if (!replacement.IsValid)
            {
                return ServiceResult<ReceivingOrderDetails>.Invalid(replacement.Errors);
            }

            ServiceError? eligibilityError = await ReceivingOrderEligibility.ValidateAsync(
                dbContext,
                order.WarehouseId,
                order.ReceivingLocationId,
                requestLines.Select(line => line.StockKeepingUnitId!.Value).ToArray(),
                nameof(Command.WarehouseId),
                nameof(Command.ReceivingLocationId),
                index => $"Lines[{index}].StockKeepingUnitId",
                cancellationToken);
            if (eligibilityError is not null)
            {
                dbContext.ChangeTracker.Clear();
                return ServiceResult<ReceivingOrderDetails>.Fail(eligibilityError);
            }

            ReceivingOrderLine[] reassignedLines =
            [
                .. order.Lines.Where(line =>
                    persistedSkuByLineId.TryGetValue(line.Id, out Guid persistedSkuId) &&
                    persistedSkuId != line.StockKeepingUnitId)
            ];
            ReceivingOrderLine[] newLines =
            [
                .. order.Lines.Where(line => !persistedSkuByLineId.ContainsKey(line.Id))
            ];
            Guid[] releasedLineIds =
            [
                .. removedLines
                    .Concat(reassignedLines)
                    .Select(line => line.Id)
                    .Distinct()
            ];

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
                        await transaction.CreateSavepointAsync(
                            SkuReconciliationSavepoint,
                            cancellationToken);
                    }

                    foreach (ReceivingOrderLine releasedLine in removedLines.Concat(reassignedLines))
                    {
                        dbContext.Entry(releasedLine).State = EntityState.Detached;
                    }

                    int releasedCount = await dbContext.ReceivingOrderLines
                        .Where(line =>
                            line.ReceivingOrderId == order.Id &&
                            releasedLineIds.Contains(line.Id))
                        .ExecuteDeleteAsync(cancellationToken);
                    if (releasedCount != releasedLineIds.Length)
                    {
                        await RollbackAsync(transaction, ownedTransaction is not null);
                        dbContext.ChangeTracker.Clear();
                        return ServiceResult<ReceivingOrderDetails>.Fail(
                            ReceivingOrderErrors.ConcurrencyConflict(
                                nameof(Command.ExpectedOrderVersion)));
                    }

                    dbContext.ReceivingOrderLines.AddRange(reassignedLines);
                }
                else
                {
                    dbContext.ReceivingOrderLines.RemoveRange(removedLines);
                }

                dbContext.ReceivingOrderLines.AddRange(newLines);
                await dbContext.SaveChangesAsync(cancellationToken);

                if (ownedTransaction is not null)
                {
                    await transaction!.CommitAsync(cancellationToken);
                }
                else if (transaction is not null)
                {
                    await transaction.ReleaseSavepointAsync(
                        SkuReconciliationSavepoint,
                        cancellationToken);
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                await RollbackAsync(transaction, ownedTransaction is not null);
                dbContext.ChangeTracker.Clear();
                return ServiceResult<ReceivingOrderDetails>.Fail(
                    ReceivingOrderErrors.ConcurrencyConflict(nameof(Command.ExpectedOrderVersion)));
            }
            catch (DbUpdateException exception)
            {
                await RollbackAsync(transaction, ownedTransaction is not null);
                dbContext.ChangeTracker.Clear();
                ServiceError? error = WmsPersistenceExceptionMapper.TryMap(exception);
                if (error is not null)
                {
                    return ServiceResult<ReceivingOrderDetails>.Fail(error);
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
                if (ownedTransaction is not null)
                {
                    await ownedTransaction.DisposeAsync();
                }
            }

            logger.LogInformation(
                "Receiving order action {Action} completed with outcome {Outcome}. Actor {ActorId}; order {ReceivingOrderId}; number {Number}; retained/new lines {LineCount}; removed lines {RemovedLineCount}.",
                "UpdateDraft", "Success", command.ActorId, order.Id, order.Number, order.Lines.Count, removedLines.Count);
            return await CreateReceivingOrder.LoadDetailsAsync(dbContext, order.Id, cancellationToken);
        }
    }

    private static Task RollbackAsync(
        IDbContextTransaction? transaction,
        bool ownsTransaction)
    {
        if (transaction is null)
        {
            return Task.CompletedTask;
        }

        return ownsTransaction
            ? transaction.RollbackAsync(CancellationToken.None)
            : transaction.RollbackToSavepointAsync(
                SkuReconciliationSavepoint,
                CancellationToken.None);
    }

    private static List<DomainValidationFailure> ValidateShape(
        Command command,
        out byte[]? expectedVersion)
    {
        List<DomainValidationFailure> errors = [];
        if (!command.ReceivingOrderId.HasValue || command.ReceivingOrderId.Value == Guid.Empty)
        {
            errors.Add(DomainValidationFailure.Required<ReceivingOrder>(
                nameof(Command.ReceivingOrderId)));
        }

        DomainValidationFailure? versionError = ReceivingOrderVersion.Parse(
            command.ExpectedOrderVersion,
            nameof(Command.ExpectedOrderVersion),
            out expectedVersion);
        if (versionError is not null)
        {
            errors.Add(versionError);
        }

        return errors;
    }
}
