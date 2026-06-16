using Microsoft.EntityFrameworkCore;
using Myrmex.AppDispatching.EventDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.SkuBarcodes;
using Myrmex.Modules.Wms.Infrastructure.Persistence;

namespace Myrmex.Modules.Wms.Catalog.Features.SkuBarcodes;

internal static class ReactivateSkuBarcode
{
    internal sealed record Command(Guid SkuBarcodeId) : ICommand<ServiceResult<SkuBarcodeDetails>>;

    internal sealed class Handler(
        WmsDbContext dbContext,
        IDomainEventDispatcher domainEventDispatcher)
        : ICommandHandler<Command, ServiceResult<SkuBarcodeDetails>>
    {
        public async Task<ServiceResult<SkuBarcodeDetails>> HandleAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            SkuBarcode? skuBarcode = await dbContext.SkuBarcodes
                .FirstOrDefaultAsync(x => x.Id == command.SkuBarcodeId, cancellationToken);

            if (skuBarcode is null)
            {
                return ServiceResult<SkuBarcodeDetails>.Fail(ServiceError.NotFound<SkuBarcode>());
            }

            skuBarcode.Reactivate();

            ServiceResult saveResult = await dbContext
                .SaveChangesAsServiceResultAsync(domainEventDispatcher, cancellationToken);

            if (!saveResult.IsSuccess)
            {
                return ServiceResult<SkuBarcodeDetails>.Fail(saveResult.Error);
            }

            return ServiceResult<SkuBarcodeDetails>.Success(SkuBarcodeDetails.From(skuBarcode));
        }
    }
}
