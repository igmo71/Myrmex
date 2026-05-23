using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Results;

namespace Myrmex.Modules.Wms.Infrastructure.Persistence;

internal static class WmsDbContextSaveExtensions
{
    public static async Task<ServiceResult> SaveChangesAsServiceResultAsync(this WmsDbContext dbContext, CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);

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