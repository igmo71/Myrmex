using Microsoft.EntityFrameworkCore;
using Myrmex.AppDispatching.EventDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;

namespace Myrmex.Modules.Wms.Topology.Features.Warehouses;

internal static class UpdateWarehouseDetails
{
    internal sealed record Command(
        Guid WarehouseId,
        string? Name,
        string? Description)
        : ICommand<ServiceResult<WarehouseDetails>>;

    internal sealed class Handler(
        WmsDbContext dbContext,
        IDomainEventDispatcher domainEventDispatcher)
        : ICommandHandler<Command, ServiceResult<WarehouseDetails>>
    {
        public async Task<ServiceResult<WarehouseDetails>> HandleAsync(Command command, CancellationToken cancellationToken = default)
        {
            Warehouse? warehouse = await dbContext.Warehouses
                .FirstOrDefaultAsync(x => x.Id == command.WarehouseId, cancellationToken);

            if (warehouse is null)
            {
                return ServiceResult<WarehouseDetails>.Fail(ServiceError.NotFound<Warehouse>());
            }

            DomainValidationResult validationResult = warehouse.UpdateDetails(
                command.Name,
                command.Description);

            if (!validationResult.IsValid)
            {
                return ServiceResult<WarehouseDetails>.Invalid(validationResult.Errors);
            }

            ServiceResult saveResult = await dbContext
                .SaveChangesAsServiceResultAsync(domainEventDispatcher, cancellationToken);

            if (!saveResult.IsSuccess)
            {
                return ServiceResult<WarehouseDetails>.Fail(saveResult.Error);
            }

            return ServiceResult<WarehouseDetails>.Success(WarehouseDetails.From(warehouse));
        }
    }
}