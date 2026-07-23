using System.Linq.Expressions;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;

namespace Myrmex.Modules.Wms.Topology.Features.StorageLocations;

internal static class StorageLocationEligibility
{
    private static readonly Expression<Func<StorageLocation, bool>> ActiveLocationPredicate =
        location => location.IsActive;

    private static readonly Expression<Func<StorageLocation, bool>> ActiveTypePredicate =
        location => location.StorageLocationType.IsActive;

    private static readonly Expression<Func<StorageLocation, bool>> ActiveStatusPredicate =
        location => location.StorageLocationStatus.IsActive;

    private static readonly Func<StorageLocation, bool> IsLocationActive =
        ActiveLocationPredicate.Compile();

    private static readonly Func<StorageLocation, bool> IsTypeActive =
        ActiveTypePredicate.Compile();

    private static readonly Func<StorageLocation, bool> IsStatusActive =
        ActiveStatusPredicate.Compile();

    public static IQueryable<StorageLocation> WhereSelectable(
        this IQueryable<StorageLocation> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source
            .Where(ActiveLocationPredicate)
            .Where(ActiveTypePredicate)
            .Where(ActiveStatusPredicate);
    }

    public static Result Evaluate(StorageLocation location)
    {
        ArgumentNullException.ThrowIfNull(location);

        return new Result(
            IsLocationActive(location),
            IsTypeActive(location),
            IsStatusActive(location));
    }

    internal readonly record struct Result(
        bool IsLocationActive,
        bool IsTypeActive,
        bool IsStatusActive)
    {
        public bool IsSelectable =>
            IsLocationActive && IsTypeActive && IsStatusActive;
    }
}
