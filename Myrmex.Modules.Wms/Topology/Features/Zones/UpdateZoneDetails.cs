using Microsoft.EntityFrameworkCore;
using Myrmex.AppDispatching.EventDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Topology.Domain.Zones;

namespace Myrmex.Modules.Wms.Topology.Features.Zones;

internal static class UpdateZoneDetails
{
    internal sealed record Command(
        Guid ZoneId,
        string? Name,
        string? Description) : ICommand<ServiceResult<ZoneDetails>>;

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

            DomainValidationResult validationResult = zone.UpdateDetails(
                command.Name,
                command.Description);

            if (!validationResult.IsValid)
            {
                return ServiceResult<ZoneDetails>.Invalid(validationResult.Errors);
            }

            ServiceResult saveResult = await dbContext
                .SaveChangesAsServiceResultAsync(domainEventDispatcher, cancellationToken);

            if (!saveResult.IsSuccess)
            {
                return ServiceResult<ZoneDetails>.Fail(saveResult.Error);
            }

            return ServiceResult<ZoneDetails>.Success(ZoneDetails.From(zone));
        }
    }
}