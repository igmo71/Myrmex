using Microsoft.EntityFrameworkCore;
using Myrmex.AppDispatching.EventDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Infrastructure.Persistence;

namespace Myrmex.Modules.Wms.Catalog.Features.UnitsOfMeasure;

internal static class CreateUnitOfMeasure
{
    internal sealed record Command(
        string? Code,
        string? Name,
        string? Symbol) : ICommand<ServiceResult<UnitOfMeasureDetails>>;

    internal sealed class Handler(
        WmsDbContext dbContext,
        IDomainEventDispatcher domainEventDispatcher)
        : ICommandHandler<Command, ServiceResult<UnitOfMeasureDetails>>
    {
        public async Task<ServiceResult<UnitOfMeasureDetails>> HandleAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            DomainValidationResult validationResult = UnitOfMeasure.Create(
                command.Code,
                command.Name,
                command.Symbol,
                out UnitOfMeasure? unitOfMeasure);

            if (!validationResult.IsValid)
            {
                return ServiceResult<UnitOfMeasureDetails>.Invalid(validationResult.Errors);
            }

            if (unitOfMeasure is null)
            {
                return ServiceResult<UnitOfMeasureDetails>.Fail(WmsErrors.UnitOfMeasure.CreateFailed);
            }

            bool codeAlreadyExists = await dbContext.UnitsOfMeasure
                .AnyAsync(x => x.Code == unitOfMeasure.Code, cancellationToken);

            if (codeAlreadyExists)
            {
                return ServiceResult<UnitOfMeasureDetails>.Fail(WmsErrors.UnitOfMeasure.CodeAlreadyExists);
            }

            dbContext.UnitsOfMeasure.Add(unitOfMeasure);

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
