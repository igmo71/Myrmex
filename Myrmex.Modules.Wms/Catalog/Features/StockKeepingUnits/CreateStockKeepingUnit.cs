using Microsoft.EntityFrameworkCore;
using Myrmex.AppDispatching.EventDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Infrastructure.Persistence;

namespace Myrmex.Modules.Wms.Catalog.Features.StockKeepingUnits;

internal static class CreateStockKeepingUnit
{
    internal sealed record Command(
        string? Code,
        string? Name,
        string? Description) : ICommand<ServiceResult<StockKeepingUnitDetails>>;

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
                out StockKeepingUnit? stockKeepingUnit);

            if (!validationResult.IsValid)
            {
                return ServiceResult<StockKeepingUnitDetails>.Invalid(validationResult.Errors);
            }

            if (stockKeepingUnit is null)
            {
                return ServiceResult<StockKeepingUnitDetails>.Fail(WmsErrors.StockKeepingUnit.CreateFailed);
            }

            bool codeAlreadyExists = await dbContext.StockKeepingUnits
                .AnyAsync(x => x.Code == stockKeepingUnit.Code, cancellationToken);

            if (codeAlreadyExists)
            {
                return ServiceResult<StockKeepingUnitDetails>.Fail(WmsErrors.StockKeepingUnit.CodeAlreadyExists);
            }

            dbContext.StockKeepingUnits.Add(stockKeepingUnit);

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
