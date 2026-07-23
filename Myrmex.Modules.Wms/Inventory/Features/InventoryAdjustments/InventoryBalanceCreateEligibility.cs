using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;
using Myrmex.Modules.Wms.Topology.Features.StorageLocations;

namespace Myrmex.Modules.Wms.Inventory.Features.InventoryAdjustments;

internal static class InventoryBalanceCreateEligibility
{
    internal static async Task<ServiceError?> ValidateAsync(
        WmsDbContext dbContext,
        InventoryBalance balance,
        CancellationToken cancellationToken)
    {
        var sku = await dbContext.StockKeepingUnits
            .AsNoTracking()
            .Include(s => s.BaseUnitOfMeasure)
            .FirstOrDefaultAsync(x => x.Id == balance.StockKeepingUnitId, cancellationToken);

        var location = await dbContext.StorageLocations
            .AsNoTracking()
            .Include(l => l.StorageLocationType)
            .Include(l => l.StorageLocationStatus)
            .FirstOrDefaultAsync(x => x.Id == balance.StorageLocationId, cancellationToken);

        if (sku is null)
        {
            return ServiceError.NotFound<InventoryBalance>("StockKeepingUnit not found", "StockKeepingUnit");
        }

        if (!sku.IsActive)
        {
            return ServiceError.Validation<InventoryBalance>("StockKeepingUnit is inactive", "StockKeepingUnit");
        }

        if (!sku.BaseUnitOfMeasure.IsActive)
        {
            return ServiceError.Validation<InventoryBalance>("BaseUnitOfMeasure is inactive", "BaseUnitOfMeasure");
        }

        if (location is null)
        {
            return ServiceError.NotFound<InventoryBalance>("StorageLocation not found", "StorageLocation");
        }

        StorageLocationEligibility.Result eligibility =
            StorageLocationEligibility.Evaluate(location);

        if (!eligibility.IsLocationActive)
        {
            return ServiceError.Validation<InventoryBalance>("StorageLocation is inactive", "StorageLocation");
        }

        if (!eligibility.IsTypeActive)
        {
            return ServiceError.Validation<InventoryBalance>("StorageLocationType is inactive", "StorageLocationType");
        }

        if (!eligibility.IsStatusActive)
        {
            return ServiceError.Validation<InventoryBalance>("StorageLocationStatus is inactive", "StorageLocationStatus");
        }

        return null;
    }
}
