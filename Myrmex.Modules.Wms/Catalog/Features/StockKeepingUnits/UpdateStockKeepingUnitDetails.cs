using Microsoft.EntityFrameworkCore;
using Myrmex.AppDispatching.EventDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Infrastructure.Persistence;

namespace Myrmex.Modules.Wms.Catalog.Features.StockKeepingUnits;

internal static class UpdateStockKeepingUnitDetails
{
    internal sealed record Command(
        Guid StockKeepingUnitId,
        string? Name,
        string? Description,
        Guid? BaseUnitOfMeasureId)
        : ICommand<ServiceResult<StockKeepingUnitDetails>>;

    internal sealed class Handler(
        WmsDbContext dbContext,
        IDomainEventDispatcher domainEventDispatcher)
        : ICommandHandler<Command, ServiceResult<StockKeepingUnitDetails>>
    {
        public async Task<ServiceResult<StockKeepingUnitDetails>> HandleAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            StockKeepingUnit? stockKeepingUnit = await dbContext.StockKeepingUnits
                .FirstOrDefaultAsync(x => x.Id == command.StockKeepingUnitId, cancellationToken);

            if (stockKeepingUnit is null)
            {
                return ServiceResult<StockKeepingUnitDetails>.Fail(WmsErrors.StockKeepingUnit.NotFound);
            }

            DomainValidationResult validationResult = StockKeepingUnit.ValidateDetails(
                command.Name,
                command.Description,
                command.BaseUnitOfMeasureId);

            if (!validationResult.IsValid)
            {
                return ServiceResult<StockKeepingUnitDetails>.Invalid(validationResult.Errors);
            }

            var baseUnitOfMeasure = await dbContext.UnitsOfMeasure
                .AsNoTracking()
                .Where(x => x.Id == command.BaseUnitOfMeasureId!.Value)
                .Select(x => new { x.IsActive })
                .FirstOrDefaultAsync(cancellationToken);

            if (baseUnitOfMeasure is null)
            {
                return ServiceResult<StockKeepingUnitDetails>.Fail(WmsErrors.StockKeepingUnit.BaseUnitOfMeasureNotFound);
            }

            if (!baseUnitOfMeasure.IsActive)
            {
                return ServiceResult<StockKeepingUnitDetails>.Fail(WmsErrors.StockKeepingUnit.BaseUnitOfMeasureInactive);
            }

            validationResult = stockKeepingUnit.UpdateDetails(
                command.Name,
                command.Description,
                command.BaseUnitOfMeasureId);

            if (!validationResult.IsValid)
            {
                return ServiceResult<StockKeepingUnitDetails>.Invalid(validationResult.Errors);
            }

            ServiceResult saveResult = await dbContext
                .SaveChangesAsServiceResultAsync(domainEventDispatcher, cancellationToken);

            if (!saveResult.IsSuccess)
            {
                return ServiceResult<StockKeepingUnitDetails>.Fail(saveResult.Error);
            }

            return ServiceResult<StockKeepingUnitDetails>.Success(StockKeepingUnitDetails.From(stockKeepingUnit));
        }
    }
}
