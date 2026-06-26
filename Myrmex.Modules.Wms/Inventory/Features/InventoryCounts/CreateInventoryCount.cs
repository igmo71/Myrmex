using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Myrmex.Core.Application;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryCounts;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;
using Myrmex.Shared.Wms.Inventory;

namespace Myrmex.Modules.Wms.Inventory.Features.InventoryCounts;

internal static class CreateInventoryCount
{
    internal sealed record Command(
        Guid? WarehouseId,
        string? Reason,
        string? ActorId) : ICommand<ServiceResult<InventoryCountDetails>>;

    internal sealed class Handler(
        WmsDbContext dbContext,
        ILogger<Handler>? logger = null)
        : ICommandHandler<Command, ServiceResult<InventoryCountDetails>>
    {
        public async Task<ServiceResult<InventoryCountDetails>> HandleAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            DomainValidationResult validationResult = InventoryCount.Create(
                command.WarehouseId,
                command.Reason,
                command.ActorId,
                out InventoryCount? count);

            if (!validationResult.IsValid)
            {
                return ServiceResult<InventoryCountDetails>.Invalid(validationResult.Errors);
            }

            InventoryCount? createdCount = count
                ?? throw new InvalidOperationException("InventoryCount.Create returned a valid result without a count.");

            Warehouse? warehouse = await dbContext.Warehouses
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == command.WarehouseId!.Value, cancellationToken);

            if (warehouse is null)
            {
                return ServiceResult<InventoryCountDetails>.Fail(
                    ServiceError.NotFound<Warehouse>("Warehouse not found", nameof(Command.WarehouseId)));
            }

            if (!warehouse.IsActive)
            {
                return ServiceResult<InventoryCountDetails>.Fail(
                    ServiceError.Validation<Warehouse>("Warehouse is inactive", nameof(Command.WarehouseId)));
            }

            dbContext.InventoryCounts.Add(createdCount);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger?.LogInformation(
                "Inventory count action {Action} completed with outcome {Outcome}. Actor {ActorId}; count {InventoryCountId}; warehouse {WarehouseId}.",
                "Create",
                "Success",
                command.ActorId,
                createdCount.Id,
                createdCount.WarehouseId);

            return await LoadDetailsAsync(dbContext, createdCount.Id, cancellationToken);
        }
    }

    internal static async Task<ServiceResult<InventoryCountDetails>> LoadDetailsAsync(
        WmsDbContext dbContext,
        Guid countId,
        CancellationToken cancellationToken)
    {
        InventoryCountDetailsData? data = await dbContext.InventoryCounts
            .AsNoTracking()
            .Where(x => x.Id == countId)
            .ProjectDetailsData()
            .SingleOrDefaultAsync(cancellationToken);

        return data is null
            ? ServiceResult<InventoryCountDetails>.Fail(
                ServiceError.Failure<InventoryCount>("InventoryCount was saved but could not be loaded"))
            : ServiceResult<InventoryCountDetails>.Success(data.ToDetails());
    }
}
