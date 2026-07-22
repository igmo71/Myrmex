using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Receiving.Domain.ReceivingOrders;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;
using Myrmex.Modules.Wms.Topology.Features.StorageLocations;
using Myrmex.Shared.Wms.Topology;

namespace Myrmex.Modules.Wms.Receiving.Features.ReceivingOrders;

internal static class ReceivingOrderEligibility
{
    public static async Task<ServiceError?> ValidateAsync(
        WmsDbContext dbContext,
        Guid warehouseId,
        Guid receivingLocationId,
        IReadOnlyList<Guid> stockKeepingUnitIds,
        string warehouseProperty,
        string receivingLocationProperty,
        Func<int, string> skuProperty,
        CancellationToken cancellationToken)
    {
        Warehouse? warehouse = await dbContext.Warehouses
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == warehouseId, cancellationToken);
        if (warehouse is null)
        {
            return ServiceError.NotFound<Warehouse>("Warehouse not found", warehouseProperty);
        }

        if (!warehouse.IsActive)
        {
            return ServiceError.Validation<Warehouse>("Warehouse is inactive", warehouseProperty);
        }

        StorageLocation? location = await dbContext.StorageLocations
            .AsNoTracking()
            .Include(x => x.StorageLocationType)
            .Include(x => x.StorageLocationStatus)
            .SingleOrDefaultAsync(x => x.Id == receivingLocationId, cancellationToken);
        if (location is null)
        {
            return ServiceError.NotFound<StorageLocation>(
                "Receiving StorageLocation not found",
                receivingLocationProperty);
        }

        if (location.WarehouseId != warehouseId)
        {
            return ReceivingOrderErrors.ReceivingLocationInvalid(
                "Receiving StorageLocation does not belong to the selected Warehouse.",
                receivingLocationProperty);
        }

        StorageLocationEligibility.Result selectability =
            StorageLocationEligibility.Evaluate(location);
        if (!selectability.IsSelectable)
        {
            return ReceivingOrderErrors.ReceivingLocationInvalid(
                "Receiving StorageLocation is not selectable for inventory.",
                receivingLocationProperty);
        }

        if (!string.Equals(
                location.StorageLocationType.Code,
                StorageLocationTypeCodes.Receiving,
                StringComparison.Ordinal))
        {
            return ReceivingOrderErrors.ReceivingLocationInvalid(
                "StorageLocationType must represent Receiving.",
                receivingLocationProperty);
        }

        Guid[] distinctSkuIds = [.. stockKeepingUnitIds.Distinct()];
        Dictionary<Guid, StockKeepingUnit> skus = await dbContext.StockKeepingUnits
            .AsNoTracking()
            .Include(x => x.BaseUnitOfMeasure)
            .Where(x => distinctSkuIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        for (int index = 0; index < stockKeepingUnitIds.Count; index++)
        {
            Guid skuId = stockKeepingUnitIds[index];
            string property = skuProperty(index);
            if (!skus.TryGetValue(skuId, out StockKeepingUnit? sku))
            {
                return ServiceError.NotFound<StockKeepingUnit>(
                    "StockKeepingUnit not found",
                    property);
            }

            if (!sku.IsActive)
            {
                return ServiceError.Validation<StockKeepingUnit>(
                    "StockKeepingUnit is inactive",
                    property);
            }

            if (!sku.BaseUnitOfMeasure.IsActive)
            {
                return ServiceError.Validation<StockKeepingUnit>(
                    "BaseUnitOfMeasure is inactive",
                    property);
            }
        }

        return null;
    }
}
