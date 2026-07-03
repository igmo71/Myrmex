using Microsoft.EntityFrameworkCore;
using Myrmex.AppDispatching.EventDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Shared.Wms.Catalog;

namespace Myrmex.Modules.Wms.Catalog.Features.StockKeepingUnits;

internal static class CreateStockKeepingUnit
{
    internal sealed record Command(
        string? Code,
        string? Name,
        string? Description,
        Guid? BaseUnitOfMeasureId) : ICommand<ServiceResult<StockKeepingUnitDetails>>;

    internal sealed class Handler(
        WmsDbContext dbContext,
        IDomainEventDispatcher domainEventDispatcher)
        : ICommandHandler<Command, ServiceResult<StockKeepingUnitDetails>>
    {
        public async Task<ServiceResult<StockKeepingUnitDetails>> HandleAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            DomainValidationResult validationResult = StockKeepingUnit.Create(
                command.Code,
                command.Name,
                command.Description,
                command.BaseUnitOfMeasureId,
                out StockKeepingUnit? stockKeepingUnit);

            if (!validationResult.IsValid)
            {
                return ServiceResult<StockKeepingUnitDetails>.Invalid(validationResult.Errors);
            }

            if (stockKeepingUnit is null)
            {
                return ServiceResult<StockKeepingUnitDetails>.Fail(ServiceError.Failure<StockKeepingUnit>("Failed to create StockKeepingUnit"));
            }

            var baseUnitOfMeasure = await dbContext.UnitsOfMeasure
                .AsNoTracking()
                .Where(x => x.Id == stockKeepingUnit.BaseUnitOfMeasureId)
                .Select(x => new { x.IsActive })
                .FirstOrDefaultAsync(cancellationToken);

            if (baseUnitOfMeasure is null)
            {
                return ServiceResult<StockKeepingUnitDetails>.Fail(ServiceError.NotFound<StockKeepingUnit>("BaseUnitOfMeasure not found", nameof(StockKeepingUnit.BaseUnitOfMeasureId)));
            }

            if (!baseUnitOfMeasure.IsActive)
            {
                return ServiceResult<StockKeepingUnitDetails>.Fail(ServiceError.Validation<StockKeepingUnit>("BaseUnitOfMeasure is inactive", nameof(StockKeepingUnit.BaseUnitOfMeasureId)));
            }

            bool codeAlreadyExists = await dbContext.StockKeepingUnits
                .AnyAsync(x => x.Code == stockKeepingUnit.Code, cancellationToken);

            if (codeAlreadyExists)
            {
                return ServiceResult<StockKeepingUnitDetails>.Fail(ServiceError.Conflict<StockKeepingUnit>("Code already exists", nameof(StockKeepingUnit.Code)));
            }

            dbContext.StockKeepingUnits.Add(stockKeepingUnit);

            ServiceResult saveResult = await dbContext
                .SaveChangesAsServiceResultAsync(domainEventDispatcher, cancellationToken);

            if (!saveResult.IsSuccess)
            {
                return ServiceResult<StockKeepingUnitDetails>.Fail(saveResult.Error);
            }

            return ServiceResult<StockKeepingUnitDetails>.Success(StockKeepingUnitDetailsMapping.From(stockKeepingUnit));
        }
    }
}
