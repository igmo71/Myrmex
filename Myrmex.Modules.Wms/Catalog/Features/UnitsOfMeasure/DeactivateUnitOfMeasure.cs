using Microsoft.EntityFrameworkCore;
using Myrmex.AppDispatching.EventDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Infrastructure.Persistence;

namespace Myrmex.Modules.Wms.Catalog.Features.UnitsOfMeasure;

internal static class DeactivateUnitOfMeasure
{
    internal sealed record Command(Guid UnitOfMeasureId) : ICommand<ServiceResult<UnitOfMeasureDetails>>;

    internal sealed class Handler(
        WmsDbContext dbContext,
        IDomainEventDispatcher domainEventDispatcher)
        : ICommandHandler<Command, ServiceResult<UnitOfMeasureDetails>>
    {
        public async Task<ServiceResult<UnitOfMeasureDetails>> HandleAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            UnitOfMeasure? unitOfMeasure = await dbContext.UnitsOfMeasure
                .FirstOrDefaultAsync(x => x.Id == command.UnitOfMeasureId, cancellationToken);

            if (unitOfMeasure is null)
            {
                return ServiceResult<UnitOfMeasureDetails>.Fail(WmsErrors.UnitOfMeasure.NotFound);
            }

            unitOfMeasure.Deactivate();

            ServiceResult saveResult = await dbContext
                .SaveChangesAsServiceResultAsync(domainEventDispatcher, cancellationToken);

            if (!saveResult.IsSuccess)
            {
                return ServiceResult<UnitOfMeasureDetails>.Fail(saveResult.Error);
            }

            return ServiceResult<UnitOfMeasureDetails>.Success(UnitOfMeasureDetails.From(unitOfMeasure));
        }
    }
}
