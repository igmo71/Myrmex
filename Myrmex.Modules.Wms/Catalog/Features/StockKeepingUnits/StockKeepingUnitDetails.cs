using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using System.Linq.Expressions;

namespace Myrmex.Modules.Wms.Catalog.Features.StockKeepingUnits;

internal sealed record StockKeepingUnitDetails(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    Guid BaseUnitOfMeasureId,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc)
{
    public static StockKeepingUnitDetails From(StockKeepingUnit stockKeepingUnit)
    {
        return new StockKeepingUnitDetails(
            stockKeepingUnit.Id,
            stockKeepingUnit.Code,
            stockKeepingUnit.Name,
            stockKeepingUnit.Description,
            stockKeepingUnit.BaseUnitOfMeasureId,
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
            stockKeepingUnit.IsActive,
            stockKeepingUnit.CreatedAtUtc,
            stockKeepingUnit.UpdatedAtUtc);
}
