using Microsoft.Extensions.Options;
using Myrmex.Integrations.OneC.Configuration;
using Myrmex.Integrations.OneC.Notifications;

namespace Myrmex.Integrations.Synchronization;

internal sealed class SynchronizationRequestFactory(
    IOptions<OneCIntegrationApiKeyOptions> options,
    TimeProvider timeProvider)
{
    public SynchronizationRequest Create(
        OneCChangeNotificationRequest notification,
        OneCChangeNotificationValidationResult validation,
        string entityType)
    {
        ArgumentNullException.ThrowIfNull(notification);
        ArgumentNullException.ThrowIfNull(validation);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);

        OneCIntegrationApiKeyOptions identity = options.Value;

        return new SynchronizationRequest
        {
            SourceSystem = identity.SourceSystem,
            SourceInstance = identity.SourceInstance!,
            EntityType = entityType,
            ExternalId = validation.RefKey.ToString("D"),
            ExternalDataVersion = validation.DataVersion,
            ExternalDocumentNumber = notification.Number,
            ExternalDocumentDate = validation.DocumentDate,
            Trigger = SynchronizationTriggers.ChangeNotification,
            Status = SynchronizationStatus.Pending,
            ReceivedAtUtc = timeProvider.GetUtcNow(),
            AttemptCount = 0
        };
    }
}
