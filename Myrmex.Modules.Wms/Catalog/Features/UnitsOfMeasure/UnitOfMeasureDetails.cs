using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Shared.Wms.Catalog;
using System.Linq.Expressions;

namespace Myrmex.Modules.Wms.Catalog.Features.UnitsOfMeasure;

internal static class UnitOfMeasureDetailsMapping
{
    public static UnitOfMeasureDetails From(UnitOfMeasure unitOfMeasure)
    {
        return new UnitOfMeasureDetails(
            unitOfMeasure.Id,
            unitOfMeasure.Code,
            unitOfMeasure.Name,
            unitOfMeasure.Symbol,
            unitOfMeasure.IsActive,
            unitOfMeasure.CreatedAtUtc,
            unitOfMeasure.UpdatedAtUtc);
    }

    public static Expression<Func<UnitOfMeasure, UnitOfMeasureDetails>> Projection =>
        unitOfMeasure => new UnitOfMeasureDetails(
            unitOfMeasure.Id,
            unitOfMeasure.Code,
            unitOfMeasure.Name,
            unitOfMeasure.Symbol,
            unitOfMeasure.IsActive,
            unitOfMeasure.CreatedAtUtc,
            unitOfMeasure.UpdatedAtUtc);
}
