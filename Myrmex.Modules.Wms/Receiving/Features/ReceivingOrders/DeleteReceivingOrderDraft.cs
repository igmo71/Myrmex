using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Myrmex.Core.Application;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Receiving.Domain.ReceivingOrders;

namespace Myrmex.Modules.Wms.Receiving.Features.ReceivingOrders;

internal static class DeleteReceivingOrderDraft
{
    internal sealed record Command(
        Guid? ReceivingOrderId,
        string? ExpectedOrderVersion,
        string? ActorId) : ICommand<ServiceResult>;

    internal sealed class Handler(WmsDbContext dbContext, ILogger<Handler> logger)
        : ICommandHandler<Command, ServiceResult>
    {
        public async Task<ServiceResult> HandleAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            List<DomainValidationFailure> shapeErrors = ValidateShape(command, out byte[]? expectedVersion);
            if (shapeErrors.Count > 0)
            {
                return ServiceResult.Invalid(shapeErrors);
            }

            ReceivingOrder? order = await dbContext.ReceivingOrders
                .Include(x => x.Lines)
                .SingleOrDefaultAsync(x => x.Id == command.ReceivingOrderId!.Value, cancellationToken);
            if (order is null)
            {
                return ServiceResult.Fail(
                    ServiceError.NotFound<ReceivingOrder>(
                        "ReceivingOrder not found",
                        nameof(Command.ReceivingOrderId)));
            }

            if (!order.RowVersion.SequenceEqual(expectedVersion!))
            {
                return ServiceResult.Fail(
                    ReceivingOrderErrors.ConcurrencyConflict(nameof(Command.ExpectedOrderVersion)));
            }

            if (order.Status != ReceivingOrderStatus.Draft)
            {
                return ServiceResult.Fail(
                    ReceivingOrderErrors.InvalidState(
                        "Only Draft receiving orders may be deleted."));
            }

            if (!order.HasValidDraftPersistenceInvariant)
            {
                return ServiceResult.Fail(
                    ReceivingOrderErrors.InvalidPersistedState(
                        "Draft receiving order has persisted lifecycle or inventory effects and cannot be deleted."));
            }

            ReceivingOrderLine[] lines = [.. order.Lines];
            dbContext.ReceivingOrderLines.RemoveRange(lines);
            dbContext.ReceivingOrders.Remove(order);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException exception)
            {
                ReceivingOrderConcurrencyDiagnostics.LogWarning(
                    logger,
                    exception,
                    "ManualDraftDelete",
                    order.Id);
                return ServiceResult.Fail(
                    ReceivingOrderErrors.ConcurrencyConflict(nameof(Command.ExpectedOrderVersion)));
            }

            logger.LogInformation(
                "Receiving order action {Action} completed with outcome {Outcome}. Actor {ActorId}; order {ReceivingOrderId}; number {Number}; removed lines {LineCount}.",
                "DeleteDraft", "Success", command.ActorId, order.Id, order.Number, lines.Length);
            return ServiceResult.Success();
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
