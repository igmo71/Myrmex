using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.DemoData.Configuration;
using Myrmex.Shared.Wms.DemoData;

namespace Myrmex.Modules.Wms.DemoData.Features;

internal static class ClearWmsDemoData
{
    internal sealed record Command(string ActorId, string? Confirmation)
        : ICommand<ServiceResult<DemoDataOperationResponse>>;

    internal sealed class Handler(
        WmsDemoDataClearService clearService,
        WmsDemoDataOperationGate operationGate,
        IOptions<WmsDemoDataOptions> options,
        ILogger<Handler> logger)
        : ICommandHandler<Command, ServiceResult<DemoDataOperationResponse>>
    {
        public async Task<ServiceResult<DemoDataOperationResponse>> HandleAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            ServiceError? guardError = Validate(options.Value, command.Confirmation);
            if (guardError is not null)
            {
                logger.LogWarning(
                    "WMS demo data clear rejected for actor {ActorId}; category {Category}.",
                    command.ActorId,
                    guardError.Code);
                return ServiceResult<DemoDataOperationResponse>.Fail(guardError);
            }

            try
            {
                using IDisposable lease = operationGate.Acquire();
                return await clearService.ClearAsync(command.ActorId, cancellationToken);
            }
            catch (WmsDemoDataOperationInProgressException)
            {
                logger.LogWarning(
                    "WMS demo data clear rejected for actor {ActorId}; another operation is active.",
                    command.ActorId);
                return ServiceResult<DemoDataOperationResponse>.Fail(
                    WmsDemoDataErrors.OperationInProgress());
            }
        }

        internal static ServiceError? Validate(
            WmsDemoDataOptions options,
            string? confirmation)
        {
            if (!options.AllowClear)
            {
                return WmsDemoDataErrors.ClearForbidden();
            }

            if (string.IsNullOrWhiteSpace(options.ClearConfirmation))
            {
                return WmsDemoDataErrors.InvalidConfiguration();
            }

            if (string.IsNullOrWhiteSpace(confirmation))
            {
                return WmsDemoDataErrors.ConfirmationRequired();
            }

            return string.Equals(
                confirmation,
                options.ClearConfirmation,
                StringComparison.Ordinal)
                ? null
                : WmsDemoDataErrors.ClearForbidden();
        }
    }
}
