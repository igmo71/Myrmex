using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Myrmex.Core.Application;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Receiving.Domain.ReceivingOrders;
using Myrmex.Shared.Wms.Receiving;

namespace Myrmex.Modules.Wms.Receiving.Features.ReceivingOrders;

internal static class ReceiveReceivingOrderLine
{
    internal sealed record Command(
        Guid? ReceivingOrderId,
        Guid? LineId,
        decimal Quantity,
        string? ExpectedOrderVersion,
        string? ActorId) : ICommand<ServiceResult<ReceivingOrderDetails>>;

    internal sealed class Handler(WmsDbContext dbContext, ILogger<Handler> logger)
        : ICommandHandler<Command, ServiceResult<ReceivingOrderDetails>>
    {
        public async Task<ServiceResult<ReceivingOrderDetails>> HandleAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            List<DomainValidationFailure> errors = Validate(command, out byte[]? version);
            if (errors.Count > 0)
            {
                return ServiceResult<ReceivingOrderDetails>.Invalid(errors);
            }

            ReceivingOrder? order = await dbContext.ReceivingOrders
                .Include(x => x.Lines)
                .SingleOrDefaultAsync(x => x.Id == command.ReceivingOrderId!.Value, cancellationToken);
            if (order is null)
            {
                return ServiceResult<ReceivingOrderDetails>.Fail(
                    ServiceError.NotFound<ReceivingOrder>("ReceivingOrder not found", nameof(Command.ReceivingOrderId)));
            }

            ReceivingOrderLine? line = order.Lines.SingleOrDefault(x => x.Id == command.LineId!.Value);
            if (line is null)
            {
                return ServiceResult<ReceivingOrderDetails>.Fail(
                    ServiceError.NotFound<ReceivingOrderLine>("ReceivingOrderLine not found", nameof(Command.LineId)));
            }

            if (!order.RowVersion.SequenceEqual(version!))
            {
                return ServiceResult<ReceivingOrderDetails>.Fail(
                    ReceivingOrderErrors.ConcurrencyConflict(nameof(Command.ExpectedOrderVersion)));
            }

            if (order.Status != ReceivingOrderStatus.InProgress)
            {
                return ServiceResult<ReceivingOrderDetails>.Fail(
                    ReceivingOrderErrors.InvalidState(
                        "Lines may be received only while the order is InProgress."));
            }

            if (command.Quantity > line.RemainingQuantity)
            {
                return ServiceResult<ReceivingOrderDetails>.Fail(
                    ReceivingOrderErrors.OverReceipt(nameof(Command.Quantity)));
            }

            DomainValidationResult result = order.Receive(line.Id, command.Quantity);
            if (!result.IsValid)
            {
                return ServiceResult<ReceivingOrderDetails>.Invalid(result.Errors);
            }

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return ServiceResult<ReceivingOrderDetails>.Fail(
                    ReceivingOrderErrors.ConcurrencyConflict(nameof(Command.ExpectedOrderVersion)));
            }

            logger.LogInformation(
                "Receiving order action {Action} completed with outcome {Outcome}. Actor {ActorId}; order {ReceivingOrderId}; line {LineId}; quantity {Quantity}.",
                "ReceiveLine", "Success", command.ActorId, order.Id, line.Id, command.Quantity);
            return await CreateReceivingOrder.LoadDetailsAsync(dbContext, order.Id, cancellationToken);
        }
    }

    private static List<DomainValidationFailure> Validate(Command command, out byte[]? version)
    {
        List<DomainValidationFailure> errors = [];
        if (!command.ReceivingOrderId.HasValue || command.ReceivingOrderId.Value == Guid.Empty)
        {
            errors.Add(DomainValidationFailure.Required<ReceivingOrder>(nameof(Command.ReceivingOrderId)));
        }
        if (!command.LineId.HasValue || command.LineId.Value == Guid.Empty)
        {
            errors.Add(DomainValidationFailure.Required<ReceivingOrderLine>(nameof(Command.LineId)));
        }
        if (command.Quantity <= 0)
        {
            errors.Add(DomainValidationFailure.IncorrectState<ReceivingOrderLine>(nameof(Command.Quantity)));
        }
        DomainValidationFailure? persistenceError =
            Myrmex.Modules.Wms.Domain.WmsQuantityPersistence.Validate<ReceivingOrderLine>(
                command.Quantity,
                nameof(Command.Quantity));
        if (persistenceError is not null)
        {
            errors.Add(persistenceError);
        }
        DomainValidationFailure? versionError = ReceivingOrderVersion.Parse(
            command.ExpectedOrderVersion,
            nameof(Command.ExpectedOrderVersion),
            out version);
        if (versionError is not null)
        {
            errors.Add(versionError);
        }
        return errors;
    }
}
