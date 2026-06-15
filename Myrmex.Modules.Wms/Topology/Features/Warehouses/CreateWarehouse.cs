using Microsoft.EntityFrameworkCore;
using Myrmex.AppDispatching.EventDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;

namespace Myrmex.Modules.Wms.Topology.Features.Warehouses;

internal static class CreateWarehouse
{
    internal sealed record Command(
        string? Code,
        string? Name,
        string? Description) : ICommand<ServiceResult<WarehouseDetails>>;

    internal sealed class Handler(
        WmsDbContext dbContext,
        IDomainEventDispatcher domainEventDispatcher)
        : ICommandHandler<Command, ServiceResult<WarehouseDetails>>
    {
        public async Task<ServiceResult<WarehouseDetails>> HandleAsync(Command command, CancellationToken cancellationToken = default)
        {
            DomainValidationResult validationResult = Warehouse.Create(
                command.Code,
                command.Name,
                command.Description,
                out Warehouse? warehouse);

            if (!validationResult.IsValid)
            {
                return ServiceResult<WarehouseDetails>.Invalid(validationResult.Errors);
            }

            if (warehouse is null)
            {
                return ServiceResult<WarehouseDetails>.Fail(ServiceError.Failure<Warehouse>("CreateFailed"));
            }

            bool codeAlreadyExists = await dbContext.Warehouses
                .AnyAsync(x => x.Code == warehouse.Code, cancellationToken);

            if (codeAlreadyExists)
            {
                return ServiceResult<WarehouseDetails>.Fail(ServiceError.Conflict<Warehouse>("code"));
            }

            dbContext.Warehouses.Add(warehouse);

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
