using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using System.Linq.Expressions;

namespace Myrmex.Modules.Wms.Catalog.Features.UnitsOfMeasure;

internal sealed record UnitOfMeasureDetails(
    Guid Id,
    string Code,
    string Name,
    string? Symbol,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc)
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
