using Microsoft.EntityFrameworkCore;
using Myrmex.AppDispatching.EventDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;
using Myrmex.Shared.Wms.Topology;

namespace Myrmex.Modules.Wms.Topology.Features.StorageLocations;

internal static class UpdateStorageLocationDetails
{
    internal sealed record Command(
        Guid StorageLocationId,
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
            StorageLocation? storageLocation = await dbContext.StorageLocations
                .FirstOrDefaultAsync(x => x.Id == command.StorageLocationId, cancellationToken);

            if (storageLocation is null)
            {
                return ServiceResult<StorageLocationDetails>.Fail(ServiceError.NotFound<StorageLocation>());
            }

            DomainValidationResult validationResult = storageLocation.UpdateDetails(
                command.Name,
                command.Description,
                command.IsPickable);

            if (!validationResult.IsValid)
            {
                return ServiceResult<StorageLocationDetails>.Invalid(validationResult.Errors);
            }

            ServiceResult saveResult = await dbContext
                .SaveChangesAsServiceResultAsync(domainEventDispatcher, cancellationToken);

            if (!saveResult.IsSuccess)
            {
                return ServiceResult<StorageLocationDetails>.Fail(saveResult.Error);
            }

            return ServiceResult<StorageLocationDetails>.Success(StorageLocationDetailsMapping.From(storageLocation));
        }
    }
}
