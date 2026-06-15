using Microsoft.EntityFrameworkCore;
using Myrmex.AppDispatching.EventDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;
using Myrmex.Modules.Wms.Topology.Domain.Zones;

namespace Myrmex.Modules.Wms.Topology.Features.StorageLocations;

internal static class CreateStorageLocation
{
    internal sealed record Command(
        Guid WarehouseId,
        Guid ZoneId,
        Guid StorageLocationTypeId,
        Guid StorageLocationStatusId,
        string? Code,
        string? Name,
        string? Description,
        bool IsPickable) : ICommand<ServiceResult<StorageLocationDetails>>;

    internal sealed class Handler(
        WmsDbContext dbContext,
        IDomainEventDispatcher domainEventDispatcher)
        : ICommandHandler<Command, ServiceResult<StorageLocationDetails>>
    {
        public async Task<ServiceResult<StorageLocationDetails>> HandleAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            DomainValidationResult validationResult = StorageLocation.Create(
                command.WarehouseId,
                command.ZoneId,
                command.StorageLocationTypeId,
                command.StorageLocationStatusId,
                command.Code,
                command.Name,
                command.Description,
                command.IsPickable,
                out StorageLocation? storageLocation);

            if (!validationResult.IsValid)
            {
                return ServiceResult<StorageLocationDetails>.Invalid(validationResult.Errors);
            }

            if (storageLocation is null)
            {
                return ServiceResult<StorageLocationDetails>.Fail(WmsErrors.StorageLocation.CreateFailed);
            }

            bool warehouseExists = await dbContext.Warehouses
                .AnyAsync(x => x.Id == storageLocation.WarehouseId, cancellationToken);

            if (!warehouseExists)
            {
                return ServiceResult<StorageLocationDetails>.Fail(ServiceError.NotFound<Warehouse>());
            }

            var zone = await dbContext.Zones
                .AsNoTracking()
                .Where(x => x.Id == storageLocation.ZoneId)
                .Select(x => new { x.Id, x.WarehouseId })
                .FirstOrDefaultAsync(cancellationToken);

            if (zone is null)
            {
                return ServiceResult<StorageLocationDetails>.Fail(ServiceError.NotFound<Zone>());
            }

            if (zone.WarehouseId != storageLocation.WarehouseId)
            {
                return ServiceResult<StorageLocationDetails>.Fail(ServiceError.Conflict<StorageLocation>(message: "Zone - Warehouse Mismatch"));
            }

            bool typeExists = await dbContext.StorageLocationTypes
                .AnyAsync(x => x.Id == storageLocation.StorageLocationTypeId && x.IsActive, cancellationToken);

            if (!typeExists)
            {
                return ServiceResult<StorageLocationDetails>.Fail(ServiceError.NotFound<StorageLocationType>());
            }

            bool statusExists = await dbContext.StorageLocationStatuses
                .AnyAsync(x => x.Id == storageLocation.StorageLocationStatusId && x.IsActive, cancellationToken);

            if (!statusExists)
            {
                return ServiceResult<StorageLocationDetails>.Fail(WmsErrors.StorageLocation.StatusNotFound);
            }

            bool codeAlreadyExists = await dbContext.StorageLocations
                .AnyAsync(x => x.WarehouseId == storageLocation.WarehouseId && x.Code == storageLocation.Code, cancellationToken);

            if (codeAlreadyExists)
            {
                return ServiceResult<StorageLocationDetails>.Fail(WmsErrors.StorageLocation.CodeAlreadyExists);
            }

            dbContext.StorageLocations.Add(storageLocation);

            ServiceResult saveResult = await dbContext
                .SaveChangesAsServiceResultAsync(domainEventDispatcher, cancellationToken);

            if (!saveResult.IsSuccess)
            {
                return ServiceResult<StorageLocationDetails>.Fail(saveResult.Error);
            }

            return ServiceResult<StorageLocationDetails>.Success(StorageLocationDetails.From(storageLocation));
        }
    }
}