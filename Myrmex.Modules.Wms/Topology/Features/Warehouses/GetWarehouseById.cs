using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;

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
                .Select(WarehouseDetails.Projection)
                .FirstOrDefaultAsync(cancellationToken);

            if (result is null)
            {
                return ServiceResult<WarehouseDetails>.Fail(ServiceError.NotFound<Warehouse>());
            }

            return ServiceResult<WarehouseDetails>.Success(result);
        }
    }
}