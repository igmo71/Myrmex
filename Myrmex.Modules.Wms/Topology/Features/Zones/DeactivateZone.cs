using Microsoft.EntityFrameworkCore;
using Myrmex.AppDispatching.EventDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Topology.Domain.Zones;
using Myrmex.Shared.Wms.Topology;

namespace Myrmex.Modules.Wms.Topology.Features.Zones;

internal static class DeactivateZone
{
    internal sealed record Command(Guid ZoneId) : ICommand<ServiceResult<ZoneDetails>>;

    internal sealed class Handler(
        WmsDbContext dbContext,
        IDomainEventDispatcher domainEventDispatcher)
        : ICommandHandler<Command, ServiceResult<ZoneDetails>>
    {
        public async Task<ServiceResult<ZoneDetails>> HandleAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            Zone? zone = await dbContext.Zones
                .FirstOrDefaultAsync(x => x.Id == command.ZoneId, cancellationToken);

            if (zone is null)
            {
                return ServiceResult<ZoneDetails>.Fail(ServiceError.NotFound<Zone>());
            }

            zone.Deactivate();

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
