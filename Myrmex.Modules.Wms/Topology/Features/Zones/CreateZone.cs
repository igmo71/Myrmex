using Microsoft.EntityFrameworkCore;
using Myrmex.AppDispatching.EventDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Topology.Domain.Zones;
using Myrmex.Shared.Wms.Topology;

namespace Myrmex.Modules.Wms.Topology.Features.Zones;

internal static class CreateZone
{
    internal sealed record Command(
        Guid WarehouseId,
        string? Code,
        string? Name,
        string? Description) : ICommand<ServiceResult<ZoneDetails>>;

    internal sealed class Handler(
        WmsDbContext dbContext,
        IDomainEventDispatcher domainEventDispatcher)
        : ICommandHandler<Command, ServiceResult<ZoneDetails>>
    {
        public async Task<ServiceResult<ZoneDetails>> HandleAsync(Command command, CancellationToken cancellationToken = default)
        {
            DomainValidationResult validationResult = Zone.Create(
                command.WarehouseId,
                command.Code,
                command.Name,
                command.Description,
                out Zone? zone);

            if (!validationResult.IsValid)
            {
                return ServiceResult<ZoneDetails>.Invalid(validationResult.Errors);
            }

            if (zone is null)
            {
                return ServiceResult<ZoneDetails>.Fail(ServiceError.Failure<Zone>("Failed to create Zone."));
            }

            bool warehouseExists = await dbContext.Warehouses
                .AnyAsync(x => x.Id == zone.WarehouseId, cancellationToken);

            if (!warehouseExists)
            {
                return ServiceResult<ZoneDetails>.Fail(ServiceError.NotFound<Zone>("Warehouse not found", "Warehouse"));
            }

            bool codeAlreadyExists = await dbContext.Zones
                .AnyAsync(x => x.WarehouseId == zone.WarehouseId && x.Code == zone.Code, cancellationToken);

            if (codeAlreadyExists)
            {
                return ServiceResult<ZoneDetails>.Fail(ServiceError.Conflict<Zone>("Code already exists", "Code"));
            }

            dbContext.Zones.Add(zone);

            ServiceResult saveResult = await dbContext
                .SaveChangesAsServiceResultAsync(domainEventDispatcher, cancellationToken);

            if (!saveResult.IsSuccess)
            {
                return ServiceResult<ZoneDetails>.Fail(saveResult.Error);
            }

            return ServiceResult<ZoneDetails>.Success(ZoneDetailsMapping.From(zone));
        }
    }
}
