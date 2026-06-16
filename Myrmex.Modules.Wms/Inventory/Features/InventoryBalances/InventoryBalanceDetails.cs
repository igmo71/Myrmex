using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;
using System.Linq.Expressions;

namespace Myrmex.Modules.Wms.Inventory.Features.InventoryBalances;

internal sealed record InventoryBalanceDetails(
    Guid Id,
    decimal Quantity,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    InventoryBalanceDetails.StockKeepingUnitInfo Sku,
    InventoryBalanceDetails.StorageLocationInfo Location)
{
    public sealed record StockKeepingUnitInfo(
        Guid Id,
        string Code,
        string Name,
        UnitOfMeasureInfo BaseUom);

    public sealed record UnitOfMeasureInfo(
        Guid Id,
        string Code,
        string? Symbol);

    public sealed record StorageLocationInfo(
        Guid Id,
        string Code,
        string Name,
        WarehouseInfo Warehouse);

    public sealed record WarehouseInfo(
        Guid Id,
        string Code,
        string Name);

    public static Expression<Func<InventoryBalance, InventoryBalanceDetails>> Project =>
        balance => new InventoryBalanceDetails(
            balance.Id,
            balance.Quantity,
            balance.CreatedAtUtc,
            balance.UpdatedAtUtc,

            new StockKeepingUnitInfo(
                balance.StockKeepingUnitId,
                balance.StockKeepingUnit.Code,
                balance.StockKeepingUnit.Name,
                new UnitOfMeasureInfo(
                    balance.StockKeepingUnit.BaseUnitOfMeasureId,
                    balance.StockKeepingUnit.BaseUnitOfMeasure.Code,
                    balance.StockKeepingUnit.BaseUnitOfMeasure.Symbol
                )
            ),

            new StorageLocationInfo(
                balance.StorageLocationId,
                balance.StorageLocation.Code,
                balance.StorageLocation.Name,
                new WarehouseInfo(
                    balance.StorageLocation.WarehouseId,
                    balance.StorageLocation.Warehouse.Code,
                    balance.StorageLocation.Warehouse.Name
                )
            )
        );
}