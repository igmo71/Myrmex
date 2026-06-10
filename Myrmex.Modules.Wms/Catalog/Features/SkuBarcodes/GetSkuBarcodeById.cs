using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;

namespace Myrmex.Modules.Wms.Catalog.Features.SkuBarcodes;

internal static class GetSkuBarcodeById
{
    internal sealed record Query(Guid SkuBarcodeId) : IQuery<ServiceResult<SkuBarcodeDetails>>;

    internal sealed class Handler(WmsDbContext dbContext) : IQueryHandler<Query, ServiceResult<SkuBarcodeDetails>>
    {
        public async Task<ServiceResult<SkuBarcodeDetails>> HandleAsync(
            Query query,
            CancellationToken cancellationToken = default)
        {
            SkuBarcodeDetails? result = await dbContext.SkuBarcodes
                .AsNoTracking()
                .Where(x => x.Id == query.SkuBarcodeId)
                .Select(SkuBarcodeDetails.Projection)
                .FirstOrDefaultAsync(cancellationToken);

            if (result is null)
            {
                return ServiceResult<SkuBarcodeDetails>.Fail(WmsErrors.SkuBarcode.NotFound);
            }

            return ServiceResult<SkuBarcodeDetails>.Success(result);
        }
    }
}
