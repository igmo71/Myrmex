using Microsoft.EntityFrameworkCore;
using Myrmex.AppDispatching.EventDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.SkuBarcodes;
using Myrmex.Modules.Wms.Infrastructure.Persistence;

namespace Myrmex.Modules.Wms.Catalog.Features.SkuBarcodes;

internal static class CreateSkuBarcode
{
    internal sealed record Command(
        Guid StockKeepingUnitId,
        string? Value,
        BarcodeSymbology Symbology,
        bool IsPrimary) : ICommand<ServiceResult<SkuBarcodeDetails>>;

    internal sealed class Handler(
        WmsDbContext dbContext,
        IDomainEventDispatcher domainEventDispatcher)
        : ICommandHandler<Command, ServiceResult<SkuBarcodeDetails>>
    {
        public async Task<ServiceResult<SkuBarcodeDetails>> HandleAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            DomainValidationResult validationResult = SkuBarcode.Create(
                command.StockKeepingUnitId,
                command.Value,
                command.Symbology,
                command.IsPrimary,
                out SkuBarcode? skuBarcode);

            if (!validationResult.IsValid)
            {
                return ServiceResult<SkuBarcodeDetails>.Invalid(validationResult.Errors);
            }

            if (skuBarcode is null)
            {
                return ServiceResult<SkuBarcodeDetails>.Fail(ServiceError.Failure<SkuBarcode>("Failed to create SkuBarcode"));
            }

            bool stockKeepingUnitExists = await dbContext.StockKeepingUnits
                .AnyAsync(x => x.Id == command.StockKeepingUnitId, cancellationToken);

            if (!stockKeepingUnitExists)
            {
                return ServiceResult<SkuBarcodeDetails>.Fail(ServiceError.NotFound<SkuBarcode>("StockKeepingUnit not found"));
            }

            bool valueAlreadyExists = await dbContext.SkuBarcodes
                .AnyAsync(x => x.Value == skuBarcode.Value, cancellationToken);

            if (valueAlreadyExists)
            {
                return ServiceResult<SkuBarcodeDetails>.Fail(ServiceError.Conflict<SkuBarcode>("Value already exists", "Value"));
            }

            if (skuBarcode.IsPrimary)
            {
                SkuBarcode[] activePrimaryBarcodes = await dbContext.SkuBarcodes
                    .Where(x =>
                        x.StockKeepingUnitId == skuBarcode.StockKeepingUnitId &&
                        x.IsActive &&
                        x.IsPrimary)
                    .ToArrayAsync(cancellationToken);

                foreach (SkuBarcode activePrimaryBarcode in activePrimaryBarcodes)
                {
                    activePrimaryBarcode.ClearPrimary();
                }
            }

            dbContext.SkuBarcodes.Add(skuBarcode);

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
