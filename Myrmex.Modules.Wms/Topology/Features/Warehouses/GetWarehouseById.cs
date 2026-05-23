using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;

namespace Myrmex.Modules.Wms.Topology.Features.Warehouses;

internal static class GetWarehouseById
{
    internal sealed record Query(Guid WarehouseId) : IQuery<ServiceResult<WarehouseDetails>>;

    internal sealed class Handler(WmsDbContext dbContext) : IQueryHandler<Query, ServiceResult<WarehouseDetails>>
    {
        public async Task<ServiceResult<WarehouseDetails>> HandleAsync(
            Query query,
            CancellationToken cancellationToken = default)
        {
            WarehouseDetails? result = await dbContext.Warehouses
                .AsNoTracking()
                .Where(x => x.Id == query.WarehouseId)
                .Select(x => x.ToDetails())
                .FirstOrDefaultAsync(cancellationToken);

            if (result is null)
            {
                return ServiceResult<WarehouseDetails>.Fail(
                    ServiceErrors.NotFound("Warehouse.NotFound", "Warehouse was not found."));
            }

            return ServiceResult<WarehouseDetails>.Success(result);
        }
    }
}