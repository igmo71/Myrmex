using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;

namespace Myrmex.Modules.Wms.Topology.Features.Warehouses;

internal static class GetWarehouseById
{
    internal sealed record Query(Guid WarehouseId) : IQuery<ServiceResult<Result>>;

    internal sealed record Result(
        Guid Id,
        string Code,
        string Name,
        string? Description,
        bool IsActive,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? UpdatedAtUtc);

    internal sealed class Handler(WmsDbContext dbContext) : IQueryHandler<Query, ServiceResult<Result>>
    {
        public async Task<ServiceResult<Result>> HandleAsync(
            Query query,
            CancellationToken cancellationToken = default)
        {
            Result? result = await dbContext.Warehouses
                .AsNoTracking()
                .Where(x => x.Id == query.WarehouseId)
                .Select(x => new Result(
                    x.Id,
                    x.Code,
                    x.Name,
                    x.Description,
                    x.IsActive,
                    x.CreatedAtUtc,
                    x.UpdatedAtUtc))
                .FirstOrDefaultAsync(cancellationToken);

            if (result is null)
            {
                return ServiceResult<Result>
                    .Fail(ServiceErrors.NotFound("Warehouse.NotFound", "Warehouse was not found."));
            }

            return ServiceResult<Result>.Success(result);
        }
    }
}
