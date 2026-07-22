using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Shared.Wms.Catalog;
using System.Linq.Expressions;

namespace Myrmex.Modules.Wms.Catalog.Features.StockKeepingUnits;

internal static class StockKeepingUnitDetailsMapping
{
    public static StockKeepingUnitDetails From(StockKeepingUnit stockKeepingUnit)
    {
        return new StockKeepingUnitDetails(
            stockKeepingUnit.Id,
            stockKeepingUnit.Code,
            stockKeepingUnit.Name,
            stockKeepingUnit.Description,
            stockKeepingUnit.BaseUnitOfMeasureId,
            stockKeepingUnit.WeightKilograms,
            stockKeepingUnit.LengthMetres,
            stockKeepingUnit.AreaSquareMetres,
            stockKeepingUnit.VolumeCubicMetres,
            stockKeepingUnit.IsActive,
            stockKeepingUnit.CreatedAtUtc,
            stockKeepingUnit.UpdatedAtUtc);
    }

    public static Expression<Func<StockKeepingUnit, StockKeepingUnitDetails>> Projection =>
        stockKeepingUnit => new StockKeepingUnitDetails(
            stockKeepingUnit.Id,
            stockKeepingUnit.Code,
            stockKeepingUnit.Name,
            stockKeepingUnit.Description,
            stockKeepingUnit.BaseUnitOfMeasureId,
            stockKeepingUnit.WeightKilograms,
            stockKeepingUnit.LengthMetres,
            stockKeepingUnit.AreaSquareMetres,
            stockKeepingUnit.VolumeCubicMetres,
            stockKeepingUnit.IsActive,
            stockKeepingUnit.CreatedAtUtc,
            stockKeepingUnit.UpdatedAtUtc);
}
