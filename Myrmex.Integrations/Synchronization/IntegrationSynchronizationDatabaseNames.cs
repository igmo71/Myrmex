namespace Myrmex.Integrations.Synchronization;

internal static class IntegrationSynchronizationDatabaseNames
{
    public const string Schema = "integration";
    public const string SynchronizationRequestsTable = "synchronization_requests";
    public const string SynchronizationRequestPrimaryKey =
        "PK_integration_synchronization_requests";
    public const string SynchronizationRequestIdempotencyUniqueIndex =
        "UX_integration_synchronization_requests_idempotency";
}
