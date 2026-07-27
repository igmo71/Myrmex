using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Myrmex.Core.Application;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Receiving.Domain.ReceivingOrders;
using Myrmex.Shared.Wms.Receiving;

namespace Myrmex.Modules.Wms.Receiving.Features.ReceivingOrders;

internal static class StartReceivingOrder
{
    internal sealed record Command(
        Guid? ReceivingOrderId,
        string? ExpectedOrderVersion,
        string? ActorId) : ICommand<ServiceResult<ReceivingOrderDetails>>;

    internal sealed class Handler(WmsDbContext dbContext, ILogger<Handler> logger)
        : ICommandHandler<Command, ServiceResult<ReceivingOrderDetails>>
    {
        public async Task<ServiceResult<ReceivingOrderDetails>> HandleAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            DomainValidationFailure? idError = ValidateId(command);
            if (idError is not null)
            {
                return ServiceResult<ReceivingOrderDetails>.Invalid([idError]);
            }

            ReceivingOrder? order = await dbContext.ReceivingOrders
                .Include(x => x.Lines)
                .SingleOrDefaultAsync(x => x.Id == command.ReceivingOrderId!.Value, cancellationToken);
            if (order is null)
            {
                return ServiceResult<ReceivingOrderDetails>.Fail(
                    ServiceError.NotFound<ReceivingOrder>("ReceivingOrder not found", nameof(Command.ReceivingOrderId)));
            }

            if (order.Status == ReceivingOrderStatus.InProgress)
            {
                return await CreateReceivingOrder.LoadDetailsAsync(dbContext, order.Id, cancellationToken);
            }

            if (order.Status == ReceivingOrderStatus.Completed)
            {
                return ServiceResult<ReceivingOrderDetails>.Fail(
                    ReceivingOrderErrors.InvalidState("Completed receiving orders cannot be started."));
            }

            DomainValidationFailure? versionError = ReceivingOrderVersion.Parse(
                command.ExpectedOrderVersion,
                nameof(Command.ExpectedOrderVersion),
                out byte[]? version);
            if (versionError is not null)
            {
                return ServiceResult<ReceivingOrderDetails>.Invalid([versionError]);
            }

            if (!order.RowVersion.SequenceEqual(version!))
            {
                return ServiceResult<ReceivingOrderDetails>.Fail(
                    ReceivingOrderErrors.ConcurrencyConflict(nameof(Command.ExpectedOrderVersion)));
            }

            ServiceError? eligibilityError = await ReceivingOrderEligibility.ValidateAsync(
                dbContext,
                order.WarehouseId,
                order.ReceivingLocationId,
                order.Lines.Select(x => x.StockKeepingUnitId).ToArray(),
                nameof(ReceivingOrder.WarehouseId),
                nameof(ReceivingOrder.ReceivingLocationId),
                index => $"Lines[{index}].StockKeepingUnitId",
                cancellationToken);
            if (eligibilityError is not null)
            {
                return ServiceResult<ReceivingOrderDetails>.Fail(eligibilityError);
            }

            DomainValidationResult result = order.Start(DateTimeOffset.UtcNow);
            if (!result.IsValid)
            {
                return ServiceResult<ReceivingOrderDetails>.Fail(
                    ReceivingOrderErrors.InvalidState("Only a valid Draft receiving order can be started."));
            }

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException exception)
            {
                ReceivingOrderConcurrencyDiagnostics.LogWarning(
                    logger,
                    exception,
                    "StartReceivingOrder",
                    order.Id);
                return ServiceResult<ReceivingOrderDetails>.Fail(
                    ReceivingOrderErrors.ConcurrencyConflict(nameof(Command.ExpectedOrderVersion)));
            }

            logger.LogInformation(
                "Receiving order action {Action} completed with outcome {Outcome}. Actor {ActorId}; order {ReceivingOrderId}.",
                "Start", "Success", command.ActorId, order.Id);
            return await CreateReceivingOrder.LoadDetailsAsync(dbContext, order.Id, cancellationToken);
        }
    }

    private static DomainValidationFailure? ValidateId(Command command)
    {
        if (!command.ReceivingOrderId.HasValue || command.ReceivingOrderId.Value == Guid.Empty)
        {
            return DomainValidationFailure.Required<ReceivingOrder>(
                nameof(Command.ReceivingOrderId));
        }

        return null;
    }
}
