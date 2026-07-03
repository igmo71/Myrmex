using Microsoft.EntityFrameworkCore;
using Myrmex.AppDispatching.EventDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Shared.Wms.Catalog;

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
                return ServiceResult<StockKeepingUnitDetails>.Fail(ServiceError.NotFound<StockKeepingUnit>());
            }

            if (!command.BaseUnitOfMeasureId.HasValue || command.BaseUnitOfMeasureId.Value == Guid.Empty)
            {
                return ServiceResult<StockKeepingUnitDetails>.Fail(ServiceError.Validation<StockKeepingUnit>("UnitOfMeasure is required", nameof(StockKeepingUnit.BaseUnitOfMeasureId)));
            }

            Guid baseUnitOfMeasureId = command.BaseUnitOfMeasureId.Value;

            ServiceResult baseUnitOfMeasureResult = await EnsureBaseUnitOfMeasureCanBeAssignedAsync(
                baseUnitOfMeasureId,
                cancellationToken);

            if (!baseUnitOfMeasureResult.IsSuccess)
            {
                return ServiceResult<StockKeepingUnitDetails>.Fail(baseUnitOfMeasureResult.Error);
            }

            DomainValidationResult validationResult = stockKeepingUnit
                .UpdateDetails(command.Name, command.Description, baseUnitOfMeasureId);

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

            return ServiceResult<StockKeepingUnitDetails>.Success(StockKeepingUnitDetailsMapping.From(stockKeepingUnit));
        }

        private async Task<ServiceResult> EnsureBaseUnitOfMeasureCanBeAssignedAsync(
            Guid baseUnitOfMeasureId,
            CancellationToken cancellationToken)
        {
            var baseUnitOfMeasure = await dbContext.UnitsOfMeasure
                .AsNoTracking()
                .Where(x => x.Id == baseUnitOfMeasureId)
                .Select(x => new { x.IsActive })
                .FirstOrDefaultAsync(cancellationToken);

            if (baseUnitOfMeasure is null)
            {
                return ServiceResult.Fail(ServiceError.NotFound<StockKeepingUnit>("BaseUnitOfMeasure not found", nameof(StockKeepingUnit.BaseUnitOfMeasureId)));
            }

            if (!baseUnitOfMeasure.IsActive)
            {
                return ServiceResult.Fail(ServiceError.Validation<StockKeepingUnit>("BaseUnitOfMeasure is inactive", nameof(StockKeepingUnit.BaseUnitOfMeasureId)));
            }

            return ServiceResult.Success();
        }
    }
}
