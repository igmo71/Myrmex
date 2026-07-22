using Microsoft.EntityFrameworkCore;
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

            UpdateReceivingOrderLineRequest[] requestLines = command.Lines?.ToArray() ?? [];
            DomainValidationResult replacement = order.ReplaceDraft(
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
                return ServiceResult<ReceivingOrderDetails>.Fail(eligibilityError);
            }

            dbContext.ReceivingOrderLines.RemoveRange(removedLines);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return ServiceResult<ReceivingOrderDetails>.Fail(
                    ReceivingOrderErrors.ConcurrencyConflict(nameof(Command.ExpectedOrderVersion)));
            }
            catch (DbUpdateException exception)
            {
                ServiceError? error = WmsPersistenceExceptionMapper.TryMap(exception);
                if (error is not null)
                {
                    return ServiceResult<ReceivingOrderDetails>.Fail(error);
                }

                throw;
            }

            logger.LogInformation(
                "Receiving order action {Action} completed with outcome {Outcome}. Actor {ActorId}; order {ReceivingOrderId}; number {Number}; retained/new lines {LineCount}; removed lines {RemovedLineCount}.",
                "UpdateDraft", "Success", command.ActorId, order.Id, order.Number, order.Lines.Count, removedLines.Count);
            return await CreateReceivingOrder.LoadDetailsAsync(dbContext, order.Id, cancellationToken);
        }
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
