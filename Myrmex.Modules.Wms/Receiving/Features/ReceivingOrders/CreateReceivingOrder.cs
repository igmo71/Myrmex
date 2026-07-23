using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Myrmex.Core.Application;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Receiving.Domain.ReceivingOrders;
using Myrmex.Shared.Wms.Receiving;

namespace Myrmex.Modules.Wms.Receiving.Features.ReceivingOrders;

internal static class CreateReceivingOrder
{
    internal sealed record Command(
        string? Number,
        Guid? WarehouseId,
        Guid? ReceivingLocationId,
        IReadOnlyList<CreateReceivingOrderLineRequest>? Lines,
        string? ActorId) : ICommand<ServiceResult<ReceivingOrderDetails>>;

    internal sealed class Handler(WmsDbContext dbContext, ILogger<Handler> logger)
        : ICommandHandler<Command, ServiceResult<ReceivingOrderDetails>>
    {
        public async Task<ServiceResult<ReceivingOrderDetails>> HandleAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            CreateReceivingOrderLineRequest[] requestLines = command.Lines?.ToArray() ?? [];
            DomainValidationResult creation = ReceivingOrder.Create(
                command.Number,
                command.WarehouseId,
                command.ReceivingLocationId,
                requestLines.Select(x => new ReceivingOrder.DraftLine(
                    null,
                    x.StockKeepingUnitId,
                    x.PlannedQuantity)),
                out ReceivingOrder? order);
            if (!creation.IsValid)
            {
                return ServiceResult<ReceivingOrderDetails>.Invalid(creation.Errors);
            }

            ReceivingOrder createdOrder = order!;
            ServiceError? eligibilityError = await ReceivingOrderEligibility.ValidateAsync(
                dbContext,
                createdOrder.WarehouseId,
                createdOrder.ReceivingLocationId,
                requestLines.Select(x => x.StockKeepingUnitId!.Value).ToArray(),
                nameof(Command.WarehouseId),
                nameof(Command.ReceivingLocationId),
                index => $"Lines[{index}].StockKeepingUnitId",
                cancellationToken);
            if (eligibilityError is not null)
            {
                return ServiceResult<ReceivingOrderDetails>.Fail(eligibilityError);
            }

            dbContext.ReceivingOrders.Add(createdOrder);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
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
                "Receiving order action {Action} completed with outcome {Outcome}. Actor {ActorId}; order {ReceivingOrderId}; number {Number}.",
                "Create", "Success", command.ActorId, createdOrder.Id, createdOrder.Number);
            return await LoadDetailsAsync(dbContext, createdOrder.Id, cancellationToken);
        }
    }

    internal static async Task<ServiceResult<ReceivingOrderDetails>> LoadDetailsAsync(
        WmsDbContext dbContext,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        ReceivingOrderDetailsData? data = await dbContext.ReceivingOrders
            .AsNoTracking()
            .Where(x => x.Id == orderId)
            .ProjectDetailsData()
            .SingleOrDefaultAsync(cancellationToken);
        return data is null
            ? ServiceResult<ReceivingOrderDetails>.Fail(
                ReceivingOrderErrors.InvalidPersistedState(
                    "ReceivingOrder was saved but its details could not be loaded."))
            : ServiceResult<ReceivingOrderDetails>.Success(data.ToDetails());
    }
}
