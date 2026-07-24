using Microsoft.EntityFrameworkCore;
using Myrmex.AppDispatching.EventDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;
using Myrmex.Modules.Wms.Topology.Features.StorageLocations;
using Myrmex.Shared.Wms.Topology;

namespace Myrmex.Modules.Wms.Topology.Features.Warehouses;

internal static class UpdateWarehouseDetails
{
    internal sealed record Command(
        Guid WarehouseId,
        string? Name,
        string? Description,
        Guid? DefaultReceivingLocationId)
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

            if (command.DefaultReceivingLocationId is Guid locationId)
            {
                StorageLocation? location = await dbContext.StorageLocations
                    .Include(x => x.StorageLocationType)
                    .Include(x => x.StorageLocationStatus)
                    .SingleOrDefaultAsync(x => x.Id == locationId, cancellationToken);
                if (location is null || location.WarehouseId != warehouse.Id ||
                    location.StorageLocationType is null ||
                    !StorageLocationEligibility.Evaluate(location).IsSelectable ||
                    !string.Equals(location.StorageLocationType.Code, StorageLocationTypeCodes.Receiving, StringComparison.Ordinal))
                {
                    return ServiceResult<WarehouseDetails>.Fail(ServiceError.Validation<Warehouse>(
                        "Default receiving location must be a selectable Receiving location in this warehouse.",
                        nameof(Command.DefaultReceivingLocationId)));
                }
            }

            warehouse.SetDefaultReceivingLocation(command.DefaultReceivingLocationId);

            ServiceResult saveResult = await dbContext
                .SaveChangesAsServiceResultAsync(domainEventDispatcher, cancellationToken);

            if (!saveResult.IsSuccess)
            {
                return ServiceResult<WarehouseDetails>.Fail(saveResult.Error);
            }

            return ServiceResult<WarehouseDetails>.Success(WarehouseDetailsMapping.From(warehouse));
        }
    }
}
