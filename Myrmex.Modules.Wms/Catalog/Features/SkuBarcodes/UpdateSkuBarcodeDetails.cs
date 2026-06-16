using Microsoft.EntityFrameworkCore;
using Myrmex.AppDispatching.EventDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.SkuBarcodes;
using Myrmex.Modules.Wms.Infrastructure.Persistence;

namespace Myrmex.Modules.Wms.Catalog.Features.SkuBarcodes;

internal static class UpdateSkuBarcodeDetails
{
    internal sealed record Command(
        Guid SkuBarcodeId,
        string? Value,
        BarcodeSymbology Symbology,
        bool IsPrimary)
        : ICommand<ServiceResult<SkuBarcodeDetails>>;

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

            string normalizedValue = SkuBarcode.NormalizeValue(command.Value);

            bool valueAlreadyExists = await dbContext.SkuBarcodes
                .AnyAsync(
                    x => x.Id != skuBarcode.Id &&
                         x.Value == normalizedValue,
                    cancellationToken);

            if (valueAlreadyExists)
            {
                return ServiceResult<SkuBarcodeDetails>.Fail(ServiceError.Conflict<SkuBarcode>("Value already exists", nameof(SkuBarcode.Value)));
            }

            DomainValidationResult validationResult = skuBarcode.UpdateDetails(
                command.Value,
                command.Symbology,
                command.IsPrimary);

            if (!validationResult.IsValid)
            {
                return ServiceResult<SkuBarcodeDetails>.Invalid(validationResult.Errors);
            }

            if (skuBarcode.IsPrimary)
            {
                SkuBarcode[] activePrimaryBarcodes = await dbContext.SkuBarcodes
                    .Where(x =>
                        x.Id != skuBarcode.Id &&
                        x.StockKeepingUnitId == skuBarcode.StockKeepingUnitId &&
                        x.IsActive &&
                        x.IsPrimary)
                    .ToArrayAsync(cancellationToken);

                foreach (SkuBarcode activePrimaryBarcode in activePrimaryBarcodes)
                {
                    activePrimaryBarcode.ClearPrimary();
                }
            }

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
