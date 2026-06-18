using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;

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

        return (sku, location) switch
        {
            (null, _) => ServiceError.NotFound<InventoryBalance>("StockKeepingUnit not found", "StockKeepingUnit"),
            ({ IsActive: false }, _) => ServiceError.Validation<InventoryBalance>("StockKeepingUnit is inactive", "StockKeepingUnit"),
            ({ BaseUnitOfMeasure.IsActive: false }, _) => ServiceError.Validation<InventoryBalance>("BaseUnitOfMeasure is inactive", "BaseUnitOfMeasure"),

            (_, null) => ServiceError.NotFound<InventoryBalance>("StorageLocation not found", "StorageLocation"),
            (_, { IsActive: false }) => ServiceError.Validation<InventoryBalance>("StorageLocation is inactive", "StorageLocation"),
            (_, { StorageLocationType.IsActive: false }) => ServiceError.Validation<InventoryBalance>("StorageLocationType is inactive", "StorageLocationType"),
            (_, { StorageLocationStatus.IsActive: false }) => ServiceError.Validation<InventoryBalance>("StorageLocationStatus is inactive", "StorageLocationStatus"),

            _ => null
        };
    }
}
