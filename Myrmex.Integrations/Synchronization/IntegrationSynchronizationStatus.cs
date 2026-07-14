namespace Myrmex.Integrations.Synchronization;

internal enum IntegrationSynchronizationStatus
{
    Pending = 0,
    Processing = 1,
    Deferred = 2,
    Completed = 3,
    Failed = 4
}
