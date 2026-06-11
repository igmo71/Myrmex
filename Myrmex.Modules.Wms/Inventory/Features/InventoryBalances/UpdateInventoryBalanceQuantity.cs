using Microsoft.EntityFrameworkCore;
using Myrmex.AppDispatching.EventDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;

namespace Myrmex.Modules.Wms.Inventory.Features.InventoryBalances;

internal static class UpdateInventoryBalanceQuantity
{
    internal sealed record Command(
        Guid InventoryBalanceId,
        decimal Quantity) : ICommand<ServiceResult<InventoryBalanceDetails>>;

    internal sealed class Handler(
        WmsDbContext dbContext,
        IDomainEventDispatcher domainEventDispatcher)
        : ICommandHandler<Command, ServiceResult<InventoryBalanceDetails>>
    {
        public async Task<ServiceResult<InventoryBalanceDetails>> HandleAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            InventoryBalance? inventoryBalance = await dbContext.InventoryBalances
                .FirstOrDefaultAsync(x => x.Id == command.InventoryBalanceId, cancellationToken);

            if (inventoryBalance is null)
            {
                return ServiceResult<InventoryBalanceDetails>.Fail(WmsErrors.InventoryBalance.NotFound);
            }

            DomainValidationResult validationResult = inventoryBalance.UpdateQuantity(command.Quantity);

            if (!validationResult.IsValid)
            {
                return ServiceResult<InventoryBalanceDetails>.Invalid(validationResult.Errors);
            }

            ServiceResult saveResult = await dbContext
                .SaveChangesAsServiceResultAsync(domainEventDispatcher, cancellationToken);

            if (!saveResult.IsSuccess)
            {
                return ServiceResult<InventoryBalanceDetails>.Fail(saveResult.Error);
            }

            IQueryable<InventoryBalance> inventoryBalanceQuery = dbContext.InventoryBalances
                .Where(x => x.Id == inventoryBalance.Id);

            InventoryBalanceDetails? details = await InventoryBalanceDetails
                .QueryFrom(dbContext, inventoryBalanceQuery)
                .SingleOrDefaultAsync(cancellationToken);

            return details is null
                ? ServiceResult<InventoryBalanceDetails>.Fail(WmsErrors.InventoryBalance.UpdateFailed)
                : ServiceResult<InventoryBalanceDetails>.Success(details);
        }
    }
}
