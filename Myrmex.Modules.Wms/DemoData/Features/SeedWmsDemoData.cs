using Microsoft.Extensions.Logging;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Shared.Wms.DemoData;

namespace Myrmex.Modules.Wms.DemoData.Features;

internal static class SeedWmsDemoData
{
    internal sealed record Command(string ActorId)
        : ICommand<ServiceResult<DemoDataOperationResponse>>;

    internal sealed class Handler(
        WmsDemoDataSeeder seeder,
        WmsDemoDataOperationGate operationGate,
        ILogger<Handler> logger)
        : ICommandHandler<Command, ServiceResult<DemoDataOperationResponse>>
    {
        public async Task<ServiceResult<DemoDataOperationResponse>> HandleAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using IDisposable lease = operationGate.Acquire();
                return await seeder.SeedAsync(command.ActorId, cancellationToken);
            }
            catch (WmsDemoDataOperationInProgressException)
            {
                logger.LogWarning(
                    "WMS demo data seed rejected for actor {ActorId}; another operation is active.",
                    command.ActorId);
                return ServiceResult<DemoDataOperationResponse>.Fail(
                    WmsDemoDataErrors.OperationInProgress());
            }
        }
    }
}
