using Microsoft.EntityFrameworkCore;
using Myrmex.AppDispatching.EventDispatching;
using Myrmex.Core.Domain;
using Myrmex.Core.Events;
using Myrmex.Core.Results;

namespace Myrmex.Modules.Wms.Infrastructure.Persistence;

internal static class WmsDbContextSaveExtensions
{
    public static async Task<ServiceResult> SaveChangesAsServiceResultAsync(
        this WmsDbContext dbContext,
        IDomainEventDispatcher domainEventDispatcher,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(domainEventDispatcher);

        List<AggregateRoot> aggregateRoots = dbContext.ChangeTracker
            .Entries<AggregateRoot>()
            .Select(x => x.Entity)
            .Where(x => x.DomainEvents.Count > 0)
            .ToList();

        List<IDomainEvent> domainEvents = aggregateRoots
            .SelectMany(x => x.DomainEvents)
            .ToList();

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);

            await domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);

            foreach (AggregateRoot aggregateRoot in aggregateRoots)
            {
                aggregateRoot.ClearDomainEvents();
            }

            return ServiceResult.Success();
        }
        catch (DbUpdateException exception)
        {
            ServiceError? error = WmsPersistenceExceptionMapper.TryMap(exception);

            if (error is not null)
            {
                return ServiceResult.Fail(error);
            }

            throw;
        }
    }
}