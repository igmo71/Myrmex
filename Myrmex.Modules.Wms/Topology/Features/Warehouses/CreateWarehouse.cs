using Microsoft.EntityFrameworkCore;
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
        string? Description) : ICommand<ServiceResult<Result>>;

    internal sealed record Result(
        Guid Id,
        string Code,
        string Name,
        string? Description,
        bool IsActive,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? UpdatedAtUtc);

    internal sealed class Handler(WmsDbContext dbContext) : ICommandHandler<Command, ServiceResult<Result>>
    {
        public async Task<ServiceResult<Result>> HandleAsync(Command command, CancellationToken cancellationToken = default)
        {
            DomainValidationResult validationResult = Warehouse.Create(
             command.Code,
             command.Name,
             command.Description,
             out Warehouse? warehouse);

            if (!validationResult.IsValid)
            {
                return ServiceResult<Result>.Invalid(validationResult.Errors);
            }

            if (warehouse is null)
            {
                return ServiceResult<Result>.Fail(
                    ServiceErrors.Failure(
                        "Warehouse.CreateFailed", "Warehouse creation failed unexpectedly."));
            }

            bool codeAlreadyExists = await dbContext.Warehouses
                .AnyAsync(x => x.Code == warehouse.Code, cancellationToken);

            if (codeAlreadyExists)
            {
                return ServiceResult<Result>.Fail(
                    ServiceErrors.Conflict(
                        "Warehouse.CodeAlreadyExists", "Warehouse with the same code already exists.", "code"));
            }

            dbContext.Warehouses.Add(warehouse);

            await dbContext.SaveChangesAsync(cancellationToken);

            Result response = new(
                warehouse.Id,
                warehouse.Code,
                warehouse.Name,
                warehouse.Description,
                warehouse.IsActive,
                warehouse.CreatedAtUtc,
                warehouse.UpdatedAtUtc);

            return ServiceResult<Result>.Success(response);
        }
    }
}
